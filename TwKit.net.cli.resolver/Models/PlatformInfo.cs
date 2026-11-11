using TwKit.net.cli.resolver.Enums;

namespace TwKit.net.cli.resolver.Models;

public readonly record struct PlatformInfo(
    OperatingSystems Os,
    CpuArchitectures Arch
);