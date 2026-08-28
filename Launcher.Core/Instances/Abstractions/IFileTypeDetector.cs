using Launcher.Core.Common.Enums;

namespace Launcher.Core.Instances.Abstractions;

public interface IFileTypeDetector
{
    Task<ParsedFileType> ParseFileAsync(string filePath);
}