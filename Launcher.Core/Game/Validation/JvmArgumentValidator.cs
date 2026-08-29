using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Launcher.Core.Game.Models;

namespace Launcher.Core.Game.Validation;

public class JvmArgumentsValidator
{
    private static readonly string[] ManagedArgumentPrefixes =
    [
        "-Xmx", "-Xms", "-Xmn", "-Xss",
        "-Djava.library.path",
        "-Djna.boot.library.path",
        "-Dorg.lwjgl.librarypath",
        "-cp", "-classpath", "--class-path",
        "-Dminecraft.launcher.brand",
        "-Dminecraft.launcher.version",
        "-Dminecraft.client.jar",
        "-Dlog4j.configurationFile",
        "-Dlibrary.directory",
        "-Dfabric.dli.env",
        "-Dfabric.dli.main"
    ];

    private readonly ConcurrentDictionary<string, JvmDryRunResult> _dryRunCache = new();

    public int ResolveJavaMajorVersion(string versionJsonPath)
    {
        if (!File.Exists(versionJsonPath))
            return 8;

        try
        {
            using var stream = File.OpenRead(versionJsonPath);
            using var doc = JsonDocument.Parse(stream);
            
            if (doc.RootElement.TryGetProperty("javaVersion", out var javaElement) &&
                javaElement.TryGetProperty("majorVersion", out var majorElement) &&
                majorElement.TryGetInt32(out int majorVersion))
            {
                return majorVersion;
            }
        }
        catch
        {
            return 8;
        }

        return 8;
    }

    public IReadOnlyList<JvmSanityIssue> CheckSemanticConflicts(string? rawArguments)
    {
        if (string.IsNullOrWhiteSpace(rawArguments))
            return [];

        var issues = new List<JvmSanityIssue>();
        var tokens = rawArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            foreach (var prefix in ManagedArgumentPrefixes)
            {
                if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new JvmSanityIssue(token, $"Flag {token} conflicts with launcher-managed parameters (RAM, classpath, or native libraries)."));
                    break;
                }
            }
        }

        return issues;
    }

    public async Task<JvmDryRunResult> ValidateViaDryRunAsync(string javaExecutablePath, string? rawArguments)
    {
        if (string.IsNullOrWhiteSpace(rawArguments))
            return new JvmDryRunResult(true, null, null);

        if (!File.Exists(javaExecutablePath))
            return new JvmDryRunResult(false, $"Java executable not found at: {javaExecutablePath}", null);

        string cacheKey = $"{javaExecutablePath}::{rawArguments}";
        if (_dryRunCache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;
        try
        {
            string validationBinary = javaExecutablePath.EndsWith("javaw.exe", StringComparison.OrdinalIgnoreCase)
                ? javaExecutablePath[..^9] + "java.exe"
                : (javaExecutablePath.EndsWith("javaw", StringComparison.OrdinalIgnoreCase)
                    ? javaExecutablePath[..^5] + "java"
                    : javaExecutablePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = File.Exists(validationBinary) ? validationBinary : javaExecutablePath,
                Arguments = $"{rawArguments} -version",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var successResult = new JvmDryRunResult(true, null, null);
                _dryRunCache[cacheKey] = successResult;
                return successResult;
            }

            string? rejectedArg = ExtractRejectedFlagFromStderr(stderr);
            var failureResult = new JvmDryRunResult(false, stderr.Trim(), rejectedArg);
            _dryRunCache[cacheKey] = failureResult;
            return failureResult;
        }
        catch (Exception ex)
        {
            return new JvmDryRunResult(false, $"Dry-run execution failed: {ex.Message}", null);
        }
    }

    public async Task<JvmValidationResult> ValidateAsync(string javaExecutablePath, string? rawArguments)
    {
        var semanticIssues = CheckSemanticConflicts(rawArguments);
        var dryRunResult = await ValidateViaDryRunAsync(javaExecutablePath, rawArguments);

        return new JvmValidationResult(
            dryRunResult.IsValid,
            dryRunResult.ErrorMessage,
            dryRunResult.RejectedArgument,
            semanticIssues);
    }

    private static string? ExtractRejectedFlagFromStderr(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
            return null;

        const string marker = "Unrecognized VM option '";
        int index = stderr.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            int start = index + marker.Length;
            int end = stderr.IndexOf('\'', start);
            if (end > start)
                return stderr.Substring(start, end - start);
        }

        return null;
    }
}