using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.Tests.UI;

public sealed class CanvasViewModelTests : IDisposable
{
    private readonly ToolOptions _options = new();
    private readonly DrawingSession _session;
    private readonly CanvasViewModel _viewModel;
    private readonly List<PixelRect> _invalidated = [];

    public CanvasViewModelTests()
    {
        _session = new DrawingSession(_options, [new PenTool(_options)], new DrawingDocument(), new UndoHistory());
        _viewModel = new CanvasViewModel(_session);
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
    public void Constructor_Null_Throws() => Assert.Throws<ArgumentNullException>(() => new CanvasViewModel(null!));
}
