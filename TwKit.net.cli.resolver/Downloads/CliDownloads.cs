using TwKit.net.cli.resolver.Enums;
using TwKit.net.cli.resolver.Interfaces;
using TwKit.net.cli.resolver.Models;

namespace TwKit.net.cli.resolver.Downloads;

public static class CliDownloads
{
    public static ICliDownload For(PlatformInfo platform) => platform.Os switch
    {
        OperatingSystems.Windows                         => new WindowsCliDownload(),
        OperatingSystems.MacOS or OperatingSystems.Linux => new UnixCliDownload(),
        _ => throw new PlatformNotSupportedException(
            "Unsupported operating system.")
    };
}