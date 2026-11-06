using System.Diagnostics.CodeAnalysis;
using System.Windows;
using DrawingApp.UI.Hosting;
using DrawingApp.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DrawingApp.UI;

[ExcludeFromCodeCoverage(Justification = "WPF application entry point.")]
public partial class App : Application
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = AppHost.Create(e.Args);
        _host.Start();

        var window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync(ShutdownTimeout).GetAwaiter().GetResult();
            _host.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }
}
