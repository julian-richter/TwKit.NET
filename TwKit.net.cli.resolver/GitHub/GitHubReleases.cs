using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using TwKit.net.cli.resolver.Exceptions;
using TwKit.net.cli.resolver.Models;

namespace TwKit.net.cli.resolver.GitHub;

internal static class GitHubReleases
{
    private const int MaxAttempts = 4;

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

        using HttpResponseMessage response = await GetWithRetryAsync(
            requestUrl,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            string releaseName = string.IsNullOrWhiteSpace(version) || version == "latest"
                ? "latest"
                : version;

            throw new TailwindReleaseNotFoundException(releaseName);
        }

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"GitHub returned {(int)response.StatusCode} for '{requestUrl}'.",
                null,
                response.StatusCode);

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
        const int maxAttempts = MaxAttempts;

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await DownloadOnceAsync(url, destinationPath, cancellationToken);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static async Task DownloadOnceAsync(
        string            url,
        string            destinationPath,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await GetWithRetryAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException($"Tailwind CSS file was not found at '{url}'.");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Download failed with HTTP {(int)response.StatusCode} for '{url}'.",
                null,
                response.StatusCode);

        long? expectedLength = response.Content.Headers.ContentLength;

        await using (FileStream file = new(
                         destinationPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None))
        {
            await response.Content.CopyToAsync(file, cancellationToken);
            await file.FlushAsync(cancellationToken);

            if (expectedLength is not null && file.Length != expectedLength.Value)
                throw new IOException(
                    $"Download of '{url}' stopped at {file.Length} of {expectedLength.Value} bytes.");
        }
    }

    private static async Task<HttpResponseMessage> GetWithRetryAsync(
        string               url,
        HttpCompletionOption completion,
        CancellationToken    cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage? response = null;

            try
            {
                response = await HttpClient.GetAsync(url, completion, cancellationToken);

                if (!IsTransient(response.StatusCode) || attempt == MaxAttempts)
                    return response;

                TimeSpan delay = RetryDelay(response, attempt);
                response.Dispose();
                response = null;
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception exception) when (attempt < MaxAttempts && IsTransient(exception, cancellationToken))
            {
                response?.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransient(HttpStatusCode status)
    {
        return status is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            return false;

        return exception is HttpRequestException or IOException or TaskCanceledException;
    }

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt)
    {
        RetryConditionHeaderValue? retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan delta &&
            delta > TimeSpan.Zero &&
            delta < TimeSpan.FromMinutes(2))
        {
            return delta;
        }

        return TimeSpan.FromSeconds(attempt);
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
