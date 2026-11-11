namespace TwKit.net.cli.resolver.Interfaces;

public interface ICliDownload
{
    string GetFileName(string            fileName);
    void   GrantExecutePermission(string filePath);
}