using System.Text.Json.Serialization;

namespace Launcher.Infrastructure.Updates.Models;

public record ReleaseAppManifest(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("fullArchive")] ReleaseArchiveMetadata FullArchive,
    [property: JsonPropertyName("deltaArchive")] ReleaseDeltaArchiveMetadata? DeltaArchive);

public record ReleaseArchiveMetadata(
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("sha256")] string Sha256);

public record ReleaseDeltaArchiveMetadata(
    [property: JsonPropertyName("baseVersion")] string BaseVersion,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("sha256")] string Sha256);