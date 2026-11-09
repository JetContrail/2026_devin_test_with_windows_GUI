using System.IO;
using DrawingApp.Core.Application.AutoSave;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.DependencyInjection;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.Core.Domain.Viewport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace DrawingApp.Tests.Core;

/// <summary>Runs alone so process-wide memory measurements are not disturbed by other tests.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnduranceTestGroup
{
    public const string Name = "Endurance";
}

/// <summary>
/// Repeats draw → Undo → Redo → zoom → save → background reload through the application services,
/// wired with <see cref="CoreServiceCollectionExtensions.AddCore"/> as in the app.
/// </summary>
internal static class EnduranceScenario
{
    public const int Iterations = 100;

    /// <summary>Retained managed memory may grow at most this much between iteration 10 and the end.</summary>
    public const long MaxMemoryGrowthBytes = 32L * 1024 * 1024;

    private const int WarmUpIterations = 10;

    public static void Run(IImageEncoder encoder, IImageDecoder decoder, string backgroundFile, string outputDirectory)
    {
        using var storage = new FakeAutoSaveStorage();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero));
        using var provider = new ServiceCollection()
            .AddSingleton(encoder)
            .AddSingleton(decoder)
            .AddSingleton<IAutoSaveStorage>(storage)
            .AddSingleton<TimeProvider>(time)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(new AutoSaveOptions { ImageFormatId = encoder.FormatId })
            .AddCore()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        var options = provider.GetRequiredService<ToolOptions>();
        var session = provider.GetRequiredService<DrawingSession>();
        var history = provider.GetRequiredService<UndoHistory>();
        var viewport = provider.GetRequiredService<ViewportState>();
        var documents = provider.GetRequiredService<DocumentService>();
        var export = provider.GetRequiredService<ExportService>();
        var autoSave = provider.GetRequiredService<AutoSaveService>();
        var drawingFile = Path.Combine(outputDirectory, "drawing" + encoder.Extensions[0]);
        var compositeFile = Path.Combine(outputDirectory, "composite" + encoder.Extensions[0]);

        documents.LoadBackground(backgroundFile);
        long baseline = 0;
        for (var i = 0; i < Iterations; i++)
        {
            if (i == WarmUpIterations)
            {
                baseline = GC.GetTotalMemory(forceFullCollection: true);
            }

            // Draw: a pen stroke, then a dot or an eraser drag over it.
            var before = session.DrawingLayer.GetBuffer().ToArray();
            var y = 10 + (i % 50);
            Stroke(session, options, ToolKind.Pen, y);
            Stroke(session, options, i % 2 == 0 ? ToolKind.Dot : ToolKind.Eraser, y);
            var drawn = session.DrawingLayer.GetBuffer().ToArray();
            Assert.Equal(2, history.UndoCount);

            session.Undo();
            session.Undo();
            Assert.Equal(before, session.DrawingLayer.GetBuffer());
            session.Redo();
            session.Redo();
            Assert.Equal(drawn, session.DrawingLayer.GetBuffer());

            viewport.ZoomAt(120, 80, 3);
            viewport.PanBy(-15, 25);
            viewport.ZoomAt(120, 80, -3);
            Assert.Equal(1.0, viewport.Scale);

            export.SaveDrawingOnly(drawingFile);
            export.SaveComposite(compositeFile);
            time.Advance(TimeSpan.FromSeconds(1));
            Assert.True(autoSave.SaveIfDirtyAsync().GetAwaiter().GetResult());

            documents.LoadBackground(backgroundFile);
            Assert.Equal(0, history.UndoCount);
        }

        var growth = GC.GetTotalMemory(forceFullCollection: true) - baseline;
        Assert.True(growth < MaxMemoryGrowthBytes, $"Retained memory grew by {growth:N0} bytes.");
        Assert.Equal(AutoSaveOptions.DefaultMaxGenerations, storage.Generations.Count);
        Assert.True(File.Exists(drawingFile));
        Assert.True(File.Exists(compositeFile));
    }

    private static void Stroke(DrawingSession session, ToolOptions options, ToolKind tool, int y)
    {
        options.ActiveTool = tool;
        session.BeginStroke(new CanvasPoint(10, y));
        session.ContinueStroke(new CanvasPoint(60, y + 5));
        session.EndStroke(new CanvasPoint(100, y));
    }
}
