using DrawingApp.Core.DependencyInjection;
using DrawingApp.Core.Domain.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace DrawingApp.Tests.Core;

public class CoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCore_RegistersToolOptionsAsSingleton()
    {
        using var provider = new ServiceCollection().AddCore()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Same(provider.GetRequiredService<ToolOptions>(), provider.GetRequiredService<ToolOptions>());
    }

    [Fact]
    public void AddCore_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CoreServiceCollectionExtensions.AddCore(null!));
}
