using System.Runtime.InteropServices;
using TwKit.net.cli.resolver.Enums;
using TwKit.net.cli.resolver.Models;

namespace TwKit.net.cli.resolver;

public static class OsUtils
{
    public static PlatformInfo GetPlatform()
    {
        OperatingSystems operatingSystem =
            OperatingSystem.IsMacOS()   ? OperatingSystems.MacOS :
            OperatingSystem.IsLinux()   ? OperatingSystems.Linux :
            OperatingSystem.IsWindows() ? OperatingSystems.Windows :
                                          throw new PlatformNotSupportedException(
                                              "Unsupported operating system.");
        
        CpuArchitectures architecture =
            RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64   => CpuArchitectures.X64,
                Architecture.Arm64 => CpuArchitectures.Arm64,
                _ => throw new PlatformNotSupportedException(
                    "Unsupported CPU architecture.")
            };
        
        return new PlatformInfo(operatingSystem, architecture);
    }
}