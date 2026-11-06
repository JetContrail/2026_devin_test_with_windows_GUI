using System.Globalization;
using System.Text.Json;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using Microsoft.Extensions.Logging;

namespace DrawingApp.Core.Application.AutoSave;

/// <summary>
/// Periodically saves the drawing layer and a manifest as a new generation when the document changed,
/// then deletes the oldest generations beyond <see cref="AutoSaveOptions.MaxGenerations"/>.
/// </summary>
/// <remarks>
/// The document is only touched by the thread that calls <see cref="RunAsync"/> / <see cref="SaveIfDirtyAsync"/>
/// (the UI thread in the app): the layer is copied there and encoded and written on the thread pool.
/// Failures are logged and retried on the next tick; they never propagate.
/// </remarks>
public sealed partial class AutoSaveService : IDisposable
{
    public const string ManifestFileName = "manifest.json";

    private const string DrawingFileStem = "drawing";

    private static readonly JsonSerializerOptions ManifestJson = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly DrawingDocument _document;
    private readonly UndoHistory _history;
    private readonly IImageEncoder _encoder;
    private readonly IAutoSaveStorage _storage;
    private readonly AutoSaveOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<AutoSaveService> _logger;

    public AutoSaveService(
        DrawingDocument document,
        UndoHistory history,
        IEnumerable<IImageEncoder> encoders,
        IAutoSaveStorage storage,
        AutoSaveOptions options,
        TimeProvider time,
        ILogger<AutoSaveService> logger)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(encoders);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.Interval, TimeSpan.Zero, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxGenerations, 1, nameof(options));

        _encoder = encoders.FirstOrDefault(e => e.FormatId == options.ImageFormatId)
            ?? throw new ArgumentException($"No encoder for format '{options.ImageFormatId}'.", nameof(encoders));
        _document = document;
        _history = history;
        _storage = storage;
        _options = options;
        _time = time;
        _logger = logger;

        _history.Changed += OnDocumentChanged;
        _document.Reset += OnDocumentChanged;
    }

    /// <summary>True when the document changed since the last successful save.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Saves every <see cref="AutoSaveOptions.Interval"/> until cancelled.</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.Interval, _time);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await SaveIfDirtyAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    /// <summary>Saves a new generation if the document changed. Returns whether a generation was written.</summary>
    public async Task<bool> SaveIfDirtyAsync(CancellationToken cancellationToken = default)
    {
        if (!IsDirty)
        {
            return false;
        }

        var savedAt = _time.GetUtcNow();
        var layer = _document.ActiveLayer.Pixels;
        var snapshot = new PixelBuffer(layer.Width, layer.Height);
        snapshot.CopyFrom(layer, layer.Bounds);
        var backgroundPath = _document.BackgroundPath;
        IsDirty = false;

        try
        {
            await Task.Run(() => Write(snapshot, backgroundPath, savedAt), cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            IsDirty = true;
            if (ex is OperationCanceledException)
            {
                throw;
            }

            LogSaveFailed(ex);
            return false;
        }

        Prune();
        return true;
    }

    public void Dispose()
    {
        _history.Changed -= OnDocumentChanged;
        _document.Reset -= OnDocumentChanged;
    }

    private void Write(PixelBuffer drawing, string? backgroundPath, DateTimeOffset savedAt)
    {
        var drawingFile = DrawingFileStem + _encoder.Extensions[0];
        using var image = new MemoryStream();
        _encoder.Encode(drawing, image);

        var manifest = new AutoSaveManifest(savedAt, drawing.Width, drawing.Height, drawingFile, backgroundPath);
        var id = savedAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        _storage.WriteGeneration(
            id,
            [
                new AutoSaveFile(drawingFile, image.ToArray()),
                new AutoSaveFile(ManifestFileName, JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestJson)),
            ]);
    }

    private void Prune()
    {
        try
        {
            // Ids are UTC timestamps, so ordinal order is chronological.
            var generations = _storage.ListGenerations().Order(StringComparer.Ordinal).ToList();
            foreach (var id in generations.Take(generations.Count - _options.MaxGenerations))
            {
                _storage.DeleteGeneration(id);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogPruneFailed(ex);
        }
    }

    private void OnDocumentChanged(object? sender, EventArgs e) => IsDirty = true;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Autosave failed; retrying on the next tick.")]
    private partial void LogSaveFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Deleting old autosave generations failed.")]
    private partial void LogPruneFailed(Exception exception);
}
