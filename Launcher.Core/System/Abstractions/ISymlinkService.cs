namespace Launcher.Core.System.Abstractions;

public interface ISymlinkService
{
    void CreateSymlinksElevated(string sourceBase, string destBase, HashSet<string> inclusions);
}