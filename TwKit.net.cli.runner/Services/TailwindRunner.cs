using System.Diagnostics;

using TwKit.net.cli.runner.Models;

namespace TwKit.net.cli.runner.Services;

public static class TailwindRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

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

        if (!Directory.Exists(options.WorkingDirectory))
            throw new DirectoryNotFoundException(
                $"The working directory was not found: {options.WorkingDirectory}");

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

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        bool succeeded = false;

        try
        {
            if (!process.WaitForExit(Timeout))
            {
                Stop(process);
                throw new TimeoutException(
                    "The Tailwind CSS CLI did not finish within 10 minutes.");
            }

            process.WaitForExit();

            string output = outputTask.GetAwaiter().GetResult();
            string error = errorTask.GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output.TrimEnd());

            if (process.ExitCode != 0)
                throw new InvalidOperationException(FailureMessage(process.ExitCode, error));

            if (!File.Exists(options.OutputCss))
                throw new InvalidOperationException(
                    $"Tailwind CSS finished without writing '{options.OutputCss}'.");

            succeeded = true;
            Console.WriteLine("Tailwind CSS finished.");
        }
        catch
        {
            if (!succeeded)
                DeleteOutput(options.OutputCss);

            throw;
        }
    }

    private static string FailureMessage(int exitCode, string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return $"Tailwind CSS CLI exited with code {exitCode}.";

        return $"Tailwind CSS CLI exited with code {exitCode}.{Environment.NewLine}{error.Trim()}";
    }

    private static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }

        process.WaitForExit();
    }

    private static void DeleteOutput(string outputCss)
    {
        try
        {
            if (File.Exists(outputCss))
                File.Delete(outputCss);
        }
        catch (IOException)
        {
        }
    }
}
