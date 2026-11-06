using DrawingApp.Core.Application.Ports;
using DrawingApp.Infrastructure.AutoSave;
using DrawingApp.Infrastructure.DependencyInjection;
using DrawingApp.Infrastructure.Imaging;
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
    public void AddInfrastructure_RegistersWicImageDecoder()
    {
        using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        Assert.IsType<WicImageDecoder>(provider.GetRequiredService<IImageDecoder>());
    }

    [Fact]
    public void AddInfrastructure_RegistersPngEncoderOnce()
    {
        using var provider = new ServiceCollection().AddInfrastructure().AddInfrastructure().BuildServiceProvider();

        Assert.IsType<PngImageEncoder>(Assert.Single(provider.GetServices<IImageEncoder>()));
    }

    [Fact]
    public void AddInfrastructure_RegistersFileAutoSaveStorageUnderLocalAppData()
    {
        using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        var storage = Assert.IsType<FileAutoSaveStorage>(provider.GetRequiredService<IAutoSaveStorage>());
        Assert.Equal(FileAutoSaveStorage.DefaultDirectory, storage.RootDirectory);
    }

    [Fact]
    public void AddInfrastructure_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => InfrastructureServiceCollectionExtensions.AddInfrastructure(null!));
}
