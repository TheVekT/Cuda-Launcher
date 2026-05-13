using Launcher.Core.Common.Enums;

namespace Launcher.Core.Instances.Abstractions;

public interface IDragDropParserService
{
    Task<ParsedFileType> ParseFileAsync(string filePath);
}