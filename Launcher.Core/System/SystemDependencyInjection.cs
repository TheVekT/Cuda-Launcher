using Launcher.Core.System.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.System;

public static class SystemDependencyInjection
{
    public static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        services.AddSingleton<ISymlinkService, SymlinkService>();
        services.AddSingleton<ISysInfoService, SysInfoService>();
        services.AddSingleton<IConnectivityService, ConnectivityService>();

        return services;
    }
}