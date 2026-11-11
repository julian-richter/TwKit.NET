using System.Text.Json.Serialization;

namespace TwKit.net.cli.resolver.GitHub;

internal sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }
    
    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}