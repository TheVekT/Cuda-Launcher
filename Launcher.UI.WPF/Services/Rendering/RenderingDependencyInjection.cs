using Launcher.UI.WPF.Services.Rendering.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.UI.WPF.Services.Rendering;

public static class RenderingDependencyInjection
{
    public static IServiceCollection AddRenderingServices(this IServiceCollection services)
    {
        services.AddSingleton<IPreviewGeneratorService, PreviewGeneratorService>();
        
        return services;
    }
}
