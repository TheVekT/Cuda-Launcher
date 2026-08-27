using System.Diagnostics;

namespace Launcher.Core.Common.Messages;

public record GameLaunchProgressMessage(double Percent, string Status);
public record GameCrashReport(int ExitCode, string StackTrace, string CrashReportFilePath);
public record GameLaunchResult(Process Process, bool? IsPerformanceModsInstalled, bool? IsEssentialApisInstalled);