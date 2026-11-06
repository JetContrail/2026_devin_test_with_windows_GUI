using System.Diagnostics.CodeAnalysis;
using System.Windows;
using DrawingApp.Core.Application.AutoSave;
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

        // Started on the UI thread so each tick snapshots the document there (see AutoSaveService).
        // StopAsync in OnExit cancels ApplicationStopping; the loop is not awaited because it resumes on this thread.
        var stopping = _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        _ = _host.Services.GetRequiredService<AutoSaveService>().RunAsync(stopping);
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
