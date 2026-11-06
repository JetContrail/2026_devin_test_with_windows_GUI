using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.Core.Domain.Viewport;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.Tests.UI;

public sealed class CanvasViewModelTests : IDisposable
{
    private readonly ToolOptions _options = new();
    private readonly DrawingSession _session;
    private readonly ViewportState _viewport = new();
    private readonly CanvasViewModel _viewModel;
    private readonly List<PixelRect> _invalidated = [];

    public CanvasViewModelTests()
    {
        _session = new DrawingSession(_options, [new PenTool(_options)], new DrawingDocument(), new UndoHistory());
        _viewModel = new CanvasViewModel(_session, _viewport);
        _viewModel.Invalidated += (_, e) => _invalidated.Add(e.Area);
    }

    public void Dispose() => _viewModel.Dispose();

    [Fact]
    public void ExposesDrawingLayerAndSize()
    {
        Assert.Same(_session.DrawingLayer, _viewModel.DrawingLayer);
        Assert.Equal(DrawingDocument.DefaultWidth, _viewModel.CanvasWidth);
        Assert.Equal(DrawingDocument.DefaultHeight, _viewModel.CanvasHeight);
    }

    [Fact]
    public void PointerDrag_DrawsWithSelectedPenSizeAndForwardsInvalidation()
    {
        _options.SetSize(ToolKind.Pen, 10);

        _viewModel.PointerDown(100, 100);
        Assert.True(_viewModel.IsStrokeActive);
        _viewModel.PointerMove(150, 100);
        _viewModel.PointerUp(200, 100);

        Assert.False(_viewModel.IsStrokeActive);
        Assert.Equal(3, _invalidated.Count);
        Assert.All(_invalidated, area => Assert.Equal(10, area.Height));
        Assert.Equal(RgbaColor.Black, _viewModel.DrawingLayer.GetPixel(175, 100));
    }

    [Fact]
    public void CancelPointer_EndsStroke()
    {
        _viewModel.PointerDown(10, 10);

        _viewModel.CancelPointer();

        Assert.False(_viewModel.IsStrokeActive);
    }

    [Fact]
    public void Dispose_StopsForwardingInvalidation()
    {
        _viewModel.Dispose();

        _session.BeginStroke(new CanvasPoint(10, 10));

        Assert.Empty(_invalidated);
    }

    [Fact]
    public void ExposesBackgroundLayer() =>
        Assert.Same(_session.Document.Background.Pixels, _viewModel.BackgroundLayer);

    [Fact]
    public void DocumentReset_RaisesLayersResetAndSizeChanges()
    {
        var resets = 0;
        var changed = new List<string?>();
        _viewModel.LayersReset += (_, _) => resets++;
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _session.Document.LoadBackground(new PixelBuffer(30, 20));

        Assert.Equal(1, resets);
        Assert.Equal(new[] { nameof(CanvasViewModel.CanvasWidth), nameof(CanvasViewModel.CanvasHeight) }, changed);
        Assert.Equal((30, 20), (_viewModel.CanvasWidth, _viewModel.CanvasHeight));
        Assert.Same(_session.DrawingLayer, _viewModel.DrawingLayer);
    }

    [Fact]
    public void Dispose_StopsForwardingReset()
    {
        var resets = 0;
        _viewModel.LayersReset += (_, _) => resets++;
        _viewModel.Dispose();

        _session.Document.LoadBackground(new PixelBuffer(30, 20));

        Assert.Equal(0, resets);
    }

    [Fact]
    public void Constructor_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CanvasViewModel(null!, _viewport));
        Assert.Throws<ArgumentNullException>(() => new CanvasViewModel(_session, null!));
    }

    [Fact]
    public void Zoom_OneNotch_ChangesScaleAroundCursorAndNotifies()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        var viewportChanged = 0;
        _viewModel.ViewportChanged += (_, _) => viewportChanged++;

        _viewModel.Zoom(100, 50, CanvasViewModel.WheelDeltaPerNotch);

        Assert.Equal(1.25, _viewModel.Scale);
        Assert.Equal(new CanvasPoint(100, 50), _viewport.ScreenToCanvas(100, 50));
        Assert.Equal((-25.0, -12.5), (_viewModel.OffsetX, _viewModel.OffsetY));
        Assert.Contains(nameof(CanvasViewModel.Scale), changed);
        Assert.Contains(nameof(CanvasViewModel.OffsetX), changed);
        Assert.Equal(1, viewportChanged);
    }

    [Fact]
    public void Zoom_PartialDeltas_AccumulateToOneNotch()
    {
        _viewModel.Zoom(0, 0, 60);
        Assert.Equal(1.0, _viewModel.Scale);

        _viewModel.Zoom(0, 0, 60);
        Assert.Equal(1.25, _viewModel.Scale);

        _viewModel.Zoom(0, 0, -2 * CanvasViewModel.WheelDeltaPerNotch);
        Assert.Equal(0.75, _viewModel.Scale);
    }

    [Fact]
    public void Pan_MovesOffsetByDragDistance()
    {
        Assert.True(_viewModel.BeginPan(10, 10));
        _viewModel.PanTo(15, 30);
        _viewModel.PanTo(25, 20);
        _viewModel.EndPan();
        _viewModel.PanTo(100, 100);

        Assert.False(_viewModel.IsPanning);
        Assert.Equal((15.0, 10.0), (_viewModel.OffsetX, _viewModel.OffsetY));
    }

    [Fact]
    public void BeginPan_DuringStrokeOrPan_IsRefused()
    {
        _viewModel.PointerDown(10, 10);
        Assert.False(_viewModel.BeginPan(0, 0));
        _viewModel.PointerUp(10, 10);

        Assert.True(_viewModel.BeginPan(0, 0));
        Assert.False(_viewModel.BeginPan(5, 5));
    }

    [Fact]
    public void LayersReset_ResetsViewportAndPan()
    {
        _viewModel.Zoom(10, 10, CanvasViewModel.WheelDeltaPerNotch);
        _viewModel.BeginPan(0, 0);

        _session.Document.LoadBackground(new PixelBuffer(30, 20));

        Assert.Equal((1.0, 0.0, 0.0), (_viewModel.Scale, _viewModel.OffsetX, _viewModel.OffsetY));
        Assert.False(_viewModel.IsPanning);
    }

    [Fact]
    public void Dispose_StopsForwardingViewportChanges()
    {
        var changed = 0;
        _viewModel.ViewportChanged += (_, _) => changed++;
        _viewModel.Dispose();

        _viewport.PanBy(1, 1);

        Assert.Equal(0, changed);
    }

    [Fact]
    public void UndoRedo_ForwardToSessionAndRaiseHistoryChanged()
    {
        var changed = 0;
        _viewModel.HistoryChanged += (_, _) => changed++;
        _viewModel.PointerDown(10, 10);
        _viewModel.PointerUp(20, 10);
        Assert.True(_viewModel.CanUndo);

        _viewModel.Undo();
        Assert.Equal(RgbaColor.Transparent, _viewModel.DrawingLayer.GetPixel(15, 10));
        Assert.True(_viewModel.CanRedo);

        _viewModel.Redo();
        Assert.Equal(RgbaColor.Black, _viewModel.DrawingLayer.GetPixel(15, 10));
        Assert.Equal(3, changed);
    }

    [Fact]
    public void Dispose_StopsForwardingHistoryChanged()
    {
        var changed = 0;
        _viewModel.HistoryChanged += (_, _) => changed++;
        _viewModel.Dispose();

        _session.BeginStroke(new CanvasPoint(10, 10));
        _session.EndStroke(new CanvasPoint(20, 10));

        Assert.Equal(0, changed);
    }
}
