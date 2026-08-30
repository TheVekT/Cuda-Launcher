using Launcher.Infrastructure.Localization.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.Infrastructure.Localization;

public static class LocalizationDependencyInjection
{
    public static IServiceCollection AddLocalizationServices(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationProvider, JsonLocalizationProvider>();
        return services;
    }
}
