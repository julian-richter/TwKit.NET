using System.Net.Http;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using TwKit.net.cli.resolver.Downloads;
using TwKit.net.cli.resolver.Exceptions;
using TwKit.net.cli.resolver.Interfaces;
using TwKit.net.cli.resolver.Models;
using TwKit.net.cli.resolver.Services;
using TwKit.net.cli.runner.Models;
using TwKit.net.cli.runner.Services;

namespace TwKit.net.blazor.Tasks;

public sealed class TailwindBuildTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string InputCss { get; set; } = "";

    [Required]
    public string OutputCss { get; set; } = "";

    [Required]
    public string WorkingDirectory { get; set; } = "";

    public string Version { get; set; } = "v4.3.4";

    public bool IntegrityCheck { get; set; }

    public bool Minify { get; set; } = true;

    public override bool Execute()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(WorkingDirectory))
                throw new ArgumentException("The project directory is required.");

            if (!Directory.Exists(WorkingDirectory))
                throw new DirectoryNotFoundException(
                    $"The project directory was not found: {WorkingDirectory}");

            string inputCss = ResolvePath(InputCss);
            string outputCss = ResolvePath(OutputCss);

            if (!File.Exists(inputCss))
                throw new FileNotFoundException(
                    $"The Tailwind CSS input file was not found: {inputCss}",
                    inputCss);

            PlatformInfo platform = OsUtils.GetPlatform();
            ICliDownload download = CliDownloads.For(platform);
            string version = string.IsNullOrWhiteSpace(Version) ? "v4.3.4" : Version.Trim();
            string executablePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "twkit",
                "tailwind",
                version,
                download.GetFileName("tailwindcss"));

            if (TailwindCli.IsReady(executablePath))
            {
                Log.LogMessage(MessageImportance.High, $"Using Tailwind CSS CLI {executablePath}");
            }
            else
            {
                Log.LogMessage(MessageImportance.High, $"Downloading Tailwind CSS CLI {version}...");
                TailwindCli.DownloadAsync(
                    platform,
                    executablePath,
                    new TailwindCliOptions
                    {
                        Version = version,
                        IntegrityCheck = IntegrityCheck
                    }).GetAwaiter().GetResult();
            }

            if (!TailwindCli.IsReady(executablePath))
                throw new InvalidOperationException(
                    $"The Tailwind CSS CLI at '{executablePath}' is not usable.");

            download.GrantExecutePermission(executablePath);

            Log.LogMessage(MessageImportance.High, "Running Tailwind CSS before Blazor compilation...");
            TailwindRunner.Run(new TailwindRunOptions(
                executablePath,
                inputCss,
                outputCss,
                WorkingDirectory,
                Minify));

            Log.LogMessage(MessageImportance.High, $"Tailwind CSS wrote {outputCss}");
            return true;
        }
        catch (Exception exception) when (IsExpected(exception))
        {
            Log.LogError(exception.Message);
            return false;
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception, showStackTrace: true);
            return false;
        }
    }

    private static bool IsExpected(Exception exception)
    {
        return exception is ArgumentException
            or DirectoryNotFoundException
            or FileNotFoundException
            or InvalidDataException
            or InvalidOperationException
            or HttpRequestException
            or TimeoutException
            or PlatformNotSupportedException
            or TailwindReleaseNotFoundException;
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A Tailwind CSS path is required.");

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(WorkingDirectory, path));
    }
}
