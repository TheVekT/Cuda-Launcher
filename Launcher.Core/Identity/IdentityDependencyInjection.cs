using Launcher.Core.Identity.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Core.Identity;

public static class IdentityDependencyInjection
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddSingleton<IAccountStorageService, AccountStorageService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IMojangProfileService, MojangProfileService>();
        
        return services;
    }
}