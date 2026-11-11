namespace TwKit.net.cli.runner.Models;

public sealed record TailwindRunOptions(
    string ExecutablePath,
    string InputCss,
    string OutputCss,
    string WorkingDirectory,
    bool Minify);
