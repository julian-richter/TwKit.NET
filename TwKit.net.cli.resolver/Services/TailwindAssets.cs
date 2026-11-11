using TwKit.net.cli.resolver.Enums;
using TwKit.net.cli.resolver.Models;

namespace TwKit.net.cli.resolver.Services;

internal static class TailwindAssets
{
    internal static string GetName(PlatformInfo platform) => platform switch
    {
        { Os: OperatingSystems.MacOS, Arch: CpuArchitectures.Arm64 } =>
            "tailwindcss-macos-arm64",
        { Os: OperatingSystems.MacOS, Arch: CpuArchitectures.X64 } =>
            "tailwindcss-macos-x64",
        { Os: OperatingSystems.Linux, Arch: CpuArchitectures.Arm64 } =>
            "tailwindcss-linux-arm64",
        { Os: OperatingSystems.Linux, Arch: CpuArchitectures.X64 } =>
            "tailwindcss-linux-x64",
        { Os: OperatingSystems.Windows, Arch: CpuArchitectures.X64 } =>
            "tailwindcss-windows-x64.exe",
        _ => throw new PlatformNotSupportedException(
            "Unsupported Tailwind CSS CLI platform.")
    };
}