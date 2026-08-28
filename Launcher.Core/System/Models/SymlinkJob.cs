// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Launcher.Core.System.Models;

public class SymlinkJob(string sourcePath, string destPath, HashSet<string> inclusions)
{
    public string SourcePath { get; set; } = sourcePath;
    public string DestPath { get; set; } = destPath;
    public HashSet<string> Inclusions { get; set; } = inclusions;
}