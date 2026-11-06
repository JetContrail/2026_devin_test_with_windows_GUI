using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DrawingApp.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Registers OS-dependent adapters (image codecs, file storage, clock).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
