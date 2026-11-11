using TwKit.net.cli.resolver.Downloads;
using TwKit.net.cli.resolver.Exceptions;
using TwKit.net.cli.resolver.Interfaces;
using TwKit.net.cli.resolver.Models;
using TwKit.net.cli.resolver.Services;

PlatformInfo platform = OsUtils.GetPlatform();
ICliDownload download = CliDownloads.For(platform);

TailwindCliOptions options = new TailwindCliOptions
{
    Version        = "latest",
    IntegrityCheck = true
};

string destinationPath = Path.Combine(
    AppContext.BaseDirectory,
    download.GetFileName("tailwindcss"));

try
{
    ResolvedTailwindCli cli = await TailwindCli.DownloadAsync(
        platform,
        destinationPath,
        options);
    
    Console.WriteLine(platform);
    Console.WriteLine(cli.Version);
    Console.WriteLine(cli.DownloadUrl);
    Console.WriteLine(destinationPath);
}
catch (TailwindReleaseNotFoundException exception)
{
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}