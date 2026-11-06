using DrawingApp.Core.Domain.Tools;
using DrawingApp.UI.Hosting;
using DrawingApp.UI.Services;
using DrawingApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DrawingApp.Tests.UI;

public class AppHostTests
{
    [Fact]
    public void Create_BuildsHostWithValidatedRegistrations()
    {
        // ValidateOnBuild verifies every registration (including MainWindow) can be constructed.
        using var host = AppHost.Create([]);

        var viewModel = host.Services.GetRequiredService<MainViewModel>();

        Assert.Same(viewModel, host.Services.GetRequiredService<MainViewModel>());
        Assert.Same(host.Services.GetRequiredService<CanvasViewModel>(), viewModel.Canvas);
        Assert.Same(host.Services.GetRequiredService<ToolOptions>(), host.Services.GetRequiredService<ToolOptions>());
        Assert.Same(TimeProvider.System, host.Services.GetRequiredService<TimeProvider>());
        Assert.IsType<WpfDialogService>(host.Services.GetRequiredService<IDialogService>());
    }

    [Fact]
    public void AddUi_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => UiServiceCollectionExtensions.AddUi(null!));
}
