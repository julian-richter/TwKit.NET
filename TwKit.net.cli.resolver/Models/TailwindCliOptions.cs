namespace TwKit.net.cli.resolver.Models;

public sealed class TailwindCliOptions
{
    public bool    IntegrityCheck { get; init; } = false;
    public string? Version        { get; init; }
}