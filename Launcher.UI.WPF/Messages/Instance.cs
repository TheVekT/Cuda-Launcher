using Launcher.Core.Instances.Models;

namespace Launcher.UI.WPF.Messages;

public record InstanceCreatedMessage(MinecraftInstance Instance);
public record InstanceUpdatedMessage(MinecraftInstance Instance);