using Launcher.Core.Models;

namespace Launcher.Core.Messages;

public record InstanceCreatedMessage(MinecraftInstance Instance);
public record InstanceUpdatedMessage(MinecraftInstance Instance);