using System.Text.Json;
using DrawingApp.Core.Application.AutoSave;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace DrawingApp.Tests.Core;

public sealed class AutoSaveServiceTests : IDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 6, 1, 2, 3, TimeSpan.Zero));
    private readonly FakeAutoSaveStorage _storage = new();
    private readonly DrawingDocument _document = new(8, 6);
    private readonly UndoHistory _history = new();
    private readonly DrawingSession _session;
    private AutoSaveService _service;

    public AutoSaveServiceTests()
    {
        var options = new ToolOptions();
        _session = new DrawingSession(options, [new PenTool(options)], _document, _history);
        _service = Create(new AutoSaveOptions { ImageFormatId = "raw" });
    }

    public void Dispose()
    {
        _service.Dispose();
        _storage.Dispose();
    }

    [Fact]
    public async Task SaveIfDirty_Unchanged_DoesNotWrite()
    {
        Assert.False(_service.IsDirty);
        Assert.False(await _service.SaveIfDirtyAsync());
        Assert.Equal(0, _storage.WriteCount);
    }

    [Fact]
    public async Task SaveIfDirty_AfterStroke_WritesDrawingAndManifestOnce()
    {
        Draw(2, 3);
        Assert.True(_service.IsDirty);

        Assert.True(await _service.SaveIfDirtyAsync());
        Assert.False(await _service.SaveIfDirtyAsync());

        var (id, files) = Assert.Single(_storage.Generations);
        Assert.Equal("20261106T010203000Z", id);
        Assert.False(_service.IsDirty);
        Assert.Equal(["drawing.raw", AutoSaveService.ManifestFileName], files.Select(f => f.Name));

        var image = ReadRaw(files[0].Content);
        Assert.Equal((8, 6), (image.Width, image.Height));
        Assert.Equal(RgbaColor.Black, image.GetPixel(2, 3));
        Assert.Equal(RgbaColor.Transparent, image.GetPixel(7, 0));

        var manifest = JsonSerializer.Deserialize<AutoSaveManifest>(files[1].Content.Span, JsonSerializerOptions.Web);
        Assert.Equal(new AutoSaveManifest(_time.GetUtcNow(), 8, 6, "drawing.raw", null), manifest);
    }

    [Fact]
    public async Task SaveIfDirty_UndoAndBackgroundLoad_MarkDirtyAndRecordBackgroundPath()
    {
        Draw(1, 1);
        await _service.SaveIfDirtyAsync();

        _session.Undo();
        Assert.True(_service.IsDirty);

        _history.Clear();
        _document.LoadBackground(new PixelBuffer(4, 4), @"C:\base.jpg");
        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.True(await _service.SaveIfDirtyAsync());

        var manifest = JsonSerializer.Deserialize<AutoSaveManifest>(
            _storage.Generations.Values.Last()[1].Content.Span, JsonSerializerOptions.Web);
        Assert.Equal((4, 4, @"C:\base.jpg"), (manifest!.Width, manifest.Height, manifest.BackgroundPath));
    }

    [Fact]
    public async Task SaveIfDirty_KeepsOnlyNewestGenerations()
    {
        _service.Dispose();
        _service = Create(new AutoSaveOptions { ImageFormatId = "raw", MaxGenerations = 3 });

        for (var i = 0; i < 5; i++)
        {
            Draw(i, 0);
            await _service.SaveIfDirtyAsync();
            _time.Advance(TimeSpan.FromMinutes(1));
        }

        Assert.Equal(
            ["20261106T010403000Z", "20261106T010503000Z", "20261106T010603000Z"],
            _storage.Generations.Keys);
    }

    [Fact]
    public void Defaults_MatchRequirements()
    {
        var options = new AutoSaveOptions();

        Assert.Equal(TimeSpan.FromSeconds(60), options.Interval);
        Assert.Equal(10, options.MaxGenerations);
        Assert.Equal("png", options.ImageFormatId);
    }

    [Fact]
    public async Task SaveIfDirty_WriteFailure_StaysDirtyAndDoesNotThrow()
    {
        Draw(1, 1);
        _storage.WriteFailure = FakeAutoSaveStorage.IoFailure();

        Assert.False(await _service.SaveIfDirtyAsync());
        Assert.True(_service.IsDirty);

        _storage.WriteFailure = null;
        Assert.True(await _service.SaveIfDirtyAsync());
        Assert.Single(_storage.Generations);
    }

    [Fact]
    public async Task SaveIfDirty_PruneFailure_StillReportsSaved()
    {
        _service.Dispose();
        _service = Create(new AutoSaveOptions { ImageFormatId = "raw", MaxGenerations = 1 });
        _storage.DeleteFailure = FakeAutoSaveStorage.IoFailure();

        Draw(1, 1);
        await _service.SaveIfDirtyAsync();
        _time.Advance(TimeSpan.FromSeconds(1));
        Draw(2, 2);

        Assert.True(await _service.SaveIfDirtyAsync());
        Assert.Equal(2, _storage.Generations.Count);
    }

    [Fact]
    public async Task RunAsync_SavesEachIntervalOnlyWhenChanged()
    {
        using var cts = new CancellationTokenSource();
        var run = _service.RunAsync(cts.Token);

        Draw(1, 1);
        _time.Advance(TimeSpan.FromSeconds(59));
        Assert.Equal(0, _storage.WriteCount);

        _time.Advance(TimeSpan.FromSeconds(1));
        await _storage.WaitForWriteAsync();

        Draw(2, 2);
        _time.Advance(TimeSpan.FromSeconds(60));
        await _storage.WaitForWriteAsync();

        cts.Cancel();
        await run;
        Assert.Equal(2, _storage.Generations.Count);
    }

    [Fact]
    public async Task RunAsync_Cancelled_Completes()
    {
        using var cts = new CancellationTokenSource();
        var run = _service.RunAsync(cts.Token);

        await cts.CancelAsync();

        await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(run.IsCompletedSuccessfully);
    }

    [Fact]
    public void Dispose_StopsTrackingChanges()
    {
        _service.Dispose();

        Draw(1, 1);

        Assert.False(_service.IsDirty);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        var encoders = new IImageEncoder[] { new FakeImageEncoder() };
        var raw = new AutoSaveOptions { ImageFormatId = "raw" };
        var logger = NullLogger<AutoSaveService>.Instance;

        Assert.Throws<ArgumentNullException>(() => new AutoSaveService(null!, _history, encoders, _storage, raw, _time, logger));
        Assert.Throws<ArgumentNullException>(() => new AutoSaveService(_document, _history, encoders, null!, raw, _time, logger));
        Assert.Throws<ArgumentNullException>(() => new AutoSaveService(_document, _history, encoders, _storage, raw, null!, logger));
        Assert.Throws<ArgumentException>(() => new AutoSaveService(_document, _history, encoders, _storage, new AutoSaveOptions(), _time, logger));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutoSaveService(
            _document, _history, encoders, _storage, new AutoSaveOptions { ImageFormatId = "raw", Interval = TimeSpan.Zero }, _time, logger));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutoSaveService(
            _document, _history, encoders, _storage, new AutoSaveOptions { ImageFormatId = "raw", MaxGenerations = 0 }, _time, logger));
    }

    private static PixelBuffer ReadRaw(ReadOnlyMemory<byte> content)
    {
        var bytes = content.ToArray();
        var image = new PixelBuffer(BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4));
        bytes.AsSpan(8).CopyTo(image.GetBuffer());
        return image;
    }

    private AutoSaveService Create(AutoSaveOptions options) =>
        new(_document, _history, [new FakeImageEncoder()], _storage, options, _time, NullLogger<AutoSaveService>.Instance);

    private void Draw(int x, int y)
    {
        _session.BeginStroke(new CanvasPoint(x, y));
        _session.EndStroke(new CanvasPoint(x, y));
    }
}
