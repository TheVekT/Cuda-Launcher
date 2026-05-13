using Launcher.Core.Instances.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Instances;

public static class InstancesDependencyInjection
{
    public static IServiceCollection AddInstancesServices(this IServiceCollection services)
    {
        services.AddSingleton<IDragDropParserService, DragDropParserService>();
        services.AddSingleton<IInstanceFileSystemService, InstanceFileSystemService>();
        services.AddSingleton<IInstanceService, InstanceService>();
        
        return services;
    }
}