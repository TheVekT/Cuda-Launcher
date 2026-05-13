using System.Diagnostics;

namespace Launcher.Core.Common.Messaging;

public record GameLaunchStateMessage(bool IsRunning, Process? Process);
public record GameLaunchProgressMessage(double Percent, string Status);