using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using TwKit.net.cli.resolver.Downloads;
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
            string inputCss = ResolvePath(InputCss);
            string outputCss = ResolvePath(OutputCss);
            PlatformInfo platform = OsUtils.GetPlatform();
            ICliDownload download = CliDownloads.For(platform);
            string version = string.IsNullOrWhiteSpace(Version) ? "v4.3.4" : Version;
            string executablePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "twkit",
                "tailwind",
                version,
                download.GetFileName("tailwindcss"));

            if (!File.Exists(executablePath))
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
            else
            {
                Log.LogMessage(MessageImportance.High, $"Using Tailwind CSS CLI {executablePath}");
            }

            download.GrantExecutePermission(executablePath);

            Log.LogMessage(MessageImportance.High, $"Running Tailwind CSS before Blazor compilation...");
            TailwindRunner.Run(new TailwindRunOptions(
                executablePath,
                inputCss,
                outputCss,
                WorkingDirectory,
                Minify));

            Log.LogMessage(MessageImportance.High, $"Tailwind CSS wrote {outputCss}");
            return true;
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception, showStackTrace: true);
            return false;
        }
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(WorkingDirectory, path));
    }
}
