using DrawingApp.UI.ViewModels;
using DrawingApp.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DrawingApp.UI.Hosting;

public static class UiServiceCollectionExtensions
{
    /// <summary>Registers views and view models.</summary>
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<CanvasViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services;
    }
}
