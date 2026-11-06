using DrawingApp.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DrawingApp.Tests.Infrastructure;

public class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_RegistersSystemTimeProvider()
    {
        using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddInfrastructure_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => InfrastructureServiceCollectionExtensions.AddInfrastructure(null!));
}
