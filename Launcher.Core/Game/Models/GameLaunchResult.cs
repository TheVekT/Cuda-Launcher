using System.Diagnostics;

namespace Launcher.Core.Game.Models;

public record GameLaunchResult(
    Process Process, 
    bool? IsPerformanceModsInstalled, 
    bool? IsEssentialApisInstalled, 
    string? SkippedGlobalJvmArguments = null);