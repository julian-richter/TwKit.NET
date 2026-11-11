using TwKit.net.cli.resolver.Interfaces;

namespace TwKit.net.cli.resolver.Downloads;

public sealed class UnixCliDownload : ICliDownload
{
    public string GetFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return fileName;
    }
    
    public void GrantExecutePermission(string filePath)
    {
        if (OperatingSystem.IsWindows())
            return;
        
        File.SetUnixFileMode(
            filePath,
            UnixFileMode.UserRead  | UnixFileMode.UserWrite    | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }
}