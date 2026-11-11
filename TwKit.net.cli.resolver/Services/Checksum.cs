using System.Security.Cryptography;

namespace TwKit.net.cli.resolver.Services;

internal static class Checksum
{
    internal static async Task VerifyAsync(
        string filePath,
        string expectedSha256,
        string reportedName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSha256);

        Console.WriteLine($"Checking SHA256 for '{reportedName}'...");

        await using FileStream stream = File.OpenRead(filePath);
        byte[] actual = await SHA256.HashDataAsync(stream, cancellationToken);
        byte[] expected = Convert.FromHexString(expectedSha256);
        string actualHex = Convert.ToHexString(actual).ToLowerInvariant();
        string expectedHex = expectedSha256.ToLowerInvariant();

        Console.WriteLine($"Downloaded hash: {actualHex}");
        Console.WriteLine($"Expected hash:   {expectedHex}");

        if (CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            Console.WriteLine("SHA256 verified.");
            return;
        }

        throw new InvalidDataException(
            $"SHA256 mismatch for '{reportedName}'. Expected {expectedHex}, got {actualHex}.");
    }

    internal static string ParseSha256(string? digest, string assetName)
    {
        const string prefix = "sha256:";

        if (digest is null || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"GitHub did not return a sha256 digest for '{assetName}'.");

        string hex = digest[prefix.Length..].Trim();

        if (hex.Length != 64 || hex.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException(
                $"GitHub returned an invalid sha256 digest for '{assetName}'.");

        return hex.ToLowerInvariant();
    }
}
