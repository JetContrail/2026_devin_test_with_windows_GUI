using DrawingApp.Core.Domain.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DrawingApp.Core.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    /// <summary>Registers domain and application services.</summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ToolOptions>();
        return services;
    }
}
