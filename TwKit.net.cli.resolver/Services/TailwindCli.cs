using TwKit.net.cli.resolver.Downloads;
using TwKit.net.cli.resolver.GitHub;
using TwKit.net.cli.resolver.Interfaces;
using TwKit.net.cli.resolver.Models;

namespace TwKit.net.cli.resolver.Services;

public static class TailwindCli
{
    public const long MinimumExecutableBytes = 1_048_576;

    private const string ReleasesUrl =
        "https://github.com/tailwindlabs/tailwindcss/releases";
    
    public static async Task<ResolvedTailwindCli> ResolveAsync(
        PlatformInfo        platform,
        TailwindCliOptions? options           = null,
        CancellationToken   cancellationToken = default)
    {
        options ??= new TailwindCliOptions();
        
        if (options.Version is not null && string.IsNullOrWhiteSpace(options.Version))
            throw new ArgumentException("Version cannot be empty.", nameof(options));
        
        string assetName = TailwindAssets.GetName(platform);
        string versionLabel = string.IsNullOrWhiteSpace(options.Version) ? "latest" : options.Version;
        Console.WriteLine($"Resolving Tailwind CSS {versionLabel} for {platform.Os} {platform.Arch}...");
        
        ReleaseInfo release = await GitHubReleases.GetAsync(
            options.Version,
            cancellationToken);
        
        ReleaseAsset asset = release.Assets.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, assetName, StringComparison.Ordinal));
        
        if (string.IsNullOrEmpty(asset.Name))
            throw new PlatformNotSupportedException(
                $"Release {release.TagName} has no '{assetName}' asset.");
        
        string? sha256 = options.IntegrityCheck
            ? Checksum.ParseSha256(asset.Digest, assetName)
            : null;
        
        Console.WriteLine($"Resolved {release.TagName}.");
        
        return new ResolvedTailwindCli(release.TagName, asset.DownloadUrl, sha256);
    }
    
    public static Task DownloadAsync(
        ResolvedTailwindCli resolved,
        string              destinationPath,
        CancellationToken   cancellationToken = default)
    {
        return DownloadAsync(
            resolved,
            destinationPath,
            CliDownloads.For(OsUtils.GetPlatform()),
            cancellationToken);
    }
    
    public static async Task<ResolvedTailwindCli> DownloadAsync(
        PlatformInfo        platform,
        string              destinationPath,
        TailwindCliOptions? options           = null,
        CancellationToken   cancellationToken = default)
    {
        ResolvedTailwindCli resolved = await ResolveAsync(
            platform,
            options,
            cancellationToken);
        
        await DownloadAsync(
            resolved,
            destinationPath,
            CliDownloads.For(platform),
            cancellationToken);
        
        return resolved;
    }
    
    private static async Task DownloadAsync(
        ResolvedTailwindCli resolved,
        string              destinationPath,
        ICliDownload        download,
        CancellationToken   cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolved.DownloadUrl);
        
        destinationPath = NormalizeDestinationPath(destinationPath, download);
        
        string? directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        
        await Task.Run(
            () => DownloadUnderLock(resolved, destinationPath, download, cancellationToken),
            cancellationToken);
    }

    public static bool IsReady(string path)
    {
        try
        {
            return File.Exists(path) && new FileInfo(path).Length >= MinimumExecutableBytes;
        }
        catch (IOException)
        {
            return false;
        }
    }
    
    private static void DownloadUnderLock(
        ResolvedTailwindCli resolved,
        string              destinationPath,
        ICliDownload        download,
        CancellationToken   cancellationToken)
    {
        using FileStream lockStream = AcquireLock(destinationPath + ".lock", cancellationToken);
        
        if (IsReady(destinationPath))
        {
            download.GrantExecutePermission(destinationPath);
            return;
        }
        
        if (File.Exists(destinationPath))
            File.Delete(destinationPath);
        
        SaveDownloadAsync(resolved, destinationPath, download, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }
    
    private static FileStream AcquireLock(string lockPath, CancellationToken cancellationToken)
    {
        long started = Environment.TickCount64;
        const long timeoutMilliseconds = 15 * 60 * 1000;
        
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException) when (Environment.TickCount64 - started < timeoutMilliseconds)
            {
                Thread.Sleep(200);
            }
            catch (IOException)
            {
                throw new TimeoutException(
                    "Timed out waiting for the Tailwind CSS CLI download.");
            }
        }
    }
    
    private static async Task SaveDownloadAsync(
        ResolvedTailwindCli resolved,
        string              destinationPath,
        ICliDownload        download,
        CancellationToken   cancellationToken)
    {
        string temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.partial";
        
        Console.WriteLine($"Downloading {resolved.DownloadUrl}");
        
        try
        {
            await GitHubReleases.DownloadAsync(
                resolved.DownloadUrl,
                temporaryPath,
                cancellationToken);
            
            Console.WriteLine("Downloaded.");

            if (!IsReady(temporaryPath))
                throw new InvalidDataException(
                    "The downloaded Tailwind CSS CLI is incomplete.");
            
            if (resolved.Sha256 is not null)
                await Checksum.VerifyAsync(
                    temporaryPath,
                    resolved.Sha256,
                    Path.GetFileName(destinationPath),
                    cancellationToken);
            
            File.Move(temporaryPath, destinationPath, overwrite: true);
            download.GrantExecutePermission(destinationPath);
        }
        catch
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            
            throw;
        }
    }
    
    private static string NormalizeDestinationPath(string destinationPath, ICliDownload download)
    {
        string fileName = download.GetFileName(Path.GetFileName(destinationPath));
        string? directory = Path.GetDirectoryName(destinationPath);
        
        return string.IsNullOrEmpty(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }
    
    public static Task VerifyChecksumAsync(
        string            filePath,
        string            expectedSha256,
        CancellationToken cancellationToken = default)
    {
        return Checksum.VerifyAsync(
            filePath,
            expectedSha256,
            Path.GetFileName(filePath),
            cancellationToken);
    }
    
    public static string GetDownloadUrl(PlatformInfo platform, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        
        string assetName = TailwindAssets.GetName(platform);
        string releasePath = version == "latest"
            ? "latest/download"
            : $"download/{version}";
        
        return $"{ReleasesUrl}/{releasePath}/{assetName}";
    }
}