namespace Launcher.Core.System.Models;

public class SymlinkJob
{
    public string SourcePath { get; set; }
    public string DestPath { get; set; }
    public HashSet<string> Inclusions { get; set; }
}