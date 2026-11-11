namespace TwKit.net.cli.resolver.Models;

internal readonly record struct ReleaseInfo(
    string                      TagName,
    IReadOnlyList<ReleaseAsset> Assets);