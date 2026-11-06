using DrawingApp.Core.DependencyInjection;
using DrawingApp.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DrawingApp.UI.Hosting;

/// <summary>Composition root: builds the Generic Host with all layers registered.</summary>
public static class AppHost
{
    public static IHost Create(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Services
            .AddCore()
            .AddInfrastructure()
            .AddUi();

        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        }));

        return builder.Build();
    }
}
