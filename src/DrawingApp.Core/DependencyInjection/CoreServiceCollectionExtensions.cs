using DrawingApp.Core.Application.AutoSave;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.Core.Domain.Viewport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DrawingApp.Core.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    /// <summary>Registers domain and application services. Ports (e.g. IImageDecoder) come from AddInfrastructure.</summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ToolOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITool, PenTool>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITool, DotTool>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITool, EraserTool>());
        services.TryAddSingleton(_ => new DrawingDocument());
        services.TryAddSingleton(_ => new UndoHistory());
        services.TryAddSingleton<DrawingSession>();
        services.TryAddSingleton<ViewportState>();
        services.TryAddSingleton<DocumentService>();
        services.TryAddSingleton<ExportService>();
        services.TryAddSingleton<AutoSaveOptions>();
        services.TryAddSingleton<AutoSaveService>();
        return services;
    }
}
