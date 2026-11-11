namespace TwKit.net.cli.resolver.Models;

public sealed record ResolvedTailwindCli(
    string  Version,
    string  DownloadUrl,
    string? Sha256);