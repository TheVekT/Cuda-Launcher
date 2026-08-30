namespace Launcher.Core.Game.Models;

public record JvmSanityIssue(string Argument, string Message);

public record JvmDryRunResult(
    bool IsValid, 
    string? ErrorMessage, 
    string? RejectedArgument);

public record JvmValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string? RejectedArgument,
    IReadOnlyList<JvmSanityIssue> SemanticIssues);