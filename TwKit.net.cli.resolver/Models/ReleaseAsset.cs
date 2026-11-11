namespace TwKit.net.cli.resolver.Models;

internal readonly record struct ReleaseAsset(
    string  Name,
    string  DownloadUrl,
    string? Digest);