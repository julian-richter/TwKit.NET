using System.Text.Json.Serialization;

namespace TwKit.net.cli.resolver.GitHub;

internal sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
    
    [JsonPropertyName("digest")]
    public string? Digest { get; set; }
}