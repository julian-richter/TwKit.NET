using System.Diagnostics;

using TwKit.net.cli.runner.Models;

namespace TwKit.net.cli.runner.Services;

public static class TailwindRunner
{
    public static void Run(TailwindRunOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ExecutablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.InputCss);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputCss);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.WorkingDirectory);

        if (!File.Exists(options.ExecutablePath))
            throw new FileNotFoundException(
                "The Tailwind CSS CLI was not found.",
                options.ExecutablePath);

        if (!File.Exists(options.InputCss))
            throw new FileNotFoundException(
                "The Tailwind CSS input file was not found.",
                options.InputCss);

        string? outputDirectory = Path.GetDirectoryName(options.OutputCss);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        Console.WriteLine($"Running Tailwind CSS {options.InputCss} -> {options.OutputCss}");

        ProcessStartInfo startInfo = new()
        {
            FileName = options.ExecutablePath,
            WorkingDirectory = options.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--input");
        startInfo.ArgumentList.Add(options.InputCss);
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(options.OutputCss);

        if (options.Minify)
            startInfo.ArgumentList.Add("--minify");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the Tailwind CSS CLI.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine(output.TrimEnd());

        if (process.ExitCode == 0)
        {
            Console.WriteLine("Tailwind CSS finished.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(error))
            Console.Error.WriteLine(error.TrimEnd());

        throw new InvalidOperationException(
            $"Tailwind CSS CLI exited with code {process.ExitCode}.");
    }
}
