using DrawingApp.Core.Application.Services;
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
        _session = new DrawingSession(_options, [new PenTool(_options)]);
        _viewModel = new CanvasViewModel(_session);
        _viewModel.Invalidated += (_, e) => _invalidated.Add(e.Area);
    }

    public void Dispose() => _viewModel.Dispose();

    [Fact]
    public void ExposesDrawingLayerAndSize()
    {
        Assert.Same(_session.DrawingLayer, _viewModel.DrawingLayer);
        Assert.Equal(DrawingSession.DefaultWidth, _viewModel.CanvasWidth);
        Assert.Equal(DrawingSession.DefaultHeight, _viewModel.CanvasHeight);
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
    public void Constructor_Null_Throws() => Assert.Throws<ArgumentNullException>(() => new CanvasViewModel(null!));
}
