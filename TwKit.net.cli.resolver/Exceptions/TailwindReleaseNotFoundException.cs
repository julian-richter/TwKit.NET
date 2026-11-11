namespace TwKit.net.cli.resolver.Exceptions;

public sealed class TailwindReleaseNotFoundException : Exception
{
    public TailwindReleaseNotFoundException(string version)
        : base($"Tailwind CSS release '{version}' was not found.")
    {
        Version = version;
    }

    public string Version { get; }
}
