using TwKit.net.cli.resolver.Interfaces;

namespace TwKit.net.cli.resolver.Downloads;

public sealed class WindowsCliDownload : ICliDownload
{
    public string GetFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        
        return fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.exe";
    }
    
    public void GrantExecutePermission(string filePath) {}
}