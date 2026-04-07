using System.Diagnostics;

namespace Launcher.Core.Messages;

public record GameLaunchStateMessage(bool IsRunning, Process? Process);
public record GameLaunchProgressMessage(double Percent, string Status);