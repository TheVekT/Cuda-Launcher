// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Launcher.Core.System.Models;

public class SymlinkJob(string sourcePath, string destPath, HashSet<string> inclusions)
{
    public string SourcePath { get; init; } = sourcePath;
    public string DestPath { get; init; } = destPath;
    public HashSet<string> Inclusions { get; set; } = inclusions;
}