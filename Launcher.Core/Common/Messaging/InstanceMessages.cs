using Launcher.Core.Instances.Models;

namespace Launcher.Core.Common.Messaging;

public record InstanceCreatedMessage(MinecraftInstance Instance);
public record InstanceUpdatedMessage(MinecraftInstance Instance);