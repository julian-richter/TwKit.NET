using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using TwKit.net.cli.resolver.Exceptions;
using TwKit.net.cli.resolver.Models;

namespace TwKit.net.cli.resolver.GitHub;

internal static class GitHubReleases
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/tailwindlabs/tailwindcss/releases/latest";
    
    private const string TaggedReleaseUrl =
        "https://api.github.com/repos/tailwindlabs/tailwindcss/releases/tags";
    
    private static readonly HttpClient HttpClient = CreateHttpClient();
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    internal static async Task<ReleaseInfo> GetAsync(
        string?           version,
        CancellationToken cancellationToken)
    {
        string requestUrl = string.IsNullOrWhiteSpace(version) || version == "latest"
            ? LatestReleaseUrl
            : $"{TaggedReleaseUrl}/{Uri.EscapeDataString(version)}";
        
        using HttpResponseMessage response = await HttpClient.GetAsync(
            requestUrl,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            string releaseName = string.IsNullOrWhiteSpace(version) || version == "latest"
                ? "latest"
                : version;

            throw new TailwindReleaseNotFoundException(releaseName);
        }

        response.EnsureSuccessStatusCode();
        
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            stream,
            JsonOptions,
            cancellationToken);
        
        if (string.IsNullOrWhiteSpace(release?.TagName))
            throw new InvalidOperationException("GitHub returned an empty Tailwind CSS release.");
        
        List<ReleaseAsset> assets = [];
        
        foreach (GitHubAsset asset in release.Assets ?? [])
        {
            if (string.IsNullOrWhiteSpace(asset.Name) ||
                string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                continue;
            }
            
            assets.Add(new ReleaseAsset(asset.Name, asset.BrowserDownloadUrl, asset.Digest));
        }
        
        return new ReleaseInfo(release.TagName, assets);
    }
    
    internal static async Task DownloadAsync(
        string            url,
        string            destinationPath,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Tailwind CSS file was not found at '{url}'.");

        response.EnsureSuccessStatusCode();
        
        await using FileStream file = File.Create(destinationPath);
        await response.Content.CopyToAsync(file, cancellationToken);
    }
    
    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TwKit.net");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }
}