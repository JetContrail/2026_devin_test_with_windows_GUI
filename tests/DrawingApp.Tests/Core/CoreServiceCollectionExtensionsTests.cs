using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.DependencyInjection;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace DrawingApp.Tests.Core;

public class CoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCore_RegistersToolOptionsAsSingleton()
    {
        using var provider = CreateServices().AddCore()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Same(provider.GetRequiredService<ToolOptions>(), provider.GetRequiredService<ToolOptions>());
    }

    [Fact]
    public void AddCore_RegistersToolsAndDrawingSession()
    {
        using var provider = CreateServices().AddCore().AddCore()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Single(provider.GetServices<ITool>(), tool => tool is PenTool);
        Assert.Single(provider.GetServices<ITool>(), tool => tool is DotTool);
        Assert.Single(provider.GetServices<ITool>(), tool => tool is EraserTool);
        Assert.Same(provider.GetRequiredService<DrawingSession>(), provider.GetRequiredService<DrawingSession>());
        Assert.Same(provider.GetRequiredService<DrawingDocument>(), provider.GetRequiredService<DrawingSession>().Document);
        Assert.Same(provider.GetRequiredService<UndoHistory>(), provider.GetRequiredService<DrawingSession>().History);
        Assert.Equal(UndoHistory.DefaultCapacity, provider.GetRequiredService<UndoHistory>().Capacity);
        Assert.NotNull(provider.GetRequiredService<DocumentService>());
        Assert.NotNull(provider.GetRequiredService<ExportService>());
    }

    [Fact]
    public void AddCore_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CoreServiceCollectionExtensions.AddCore(null!));

    // Ports are provided by AddInfrastructure in the app.
    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IImageDecoder>(new FakeImageDecoder());
        return services;
    }
}
