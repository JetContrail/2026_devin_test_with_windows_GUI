using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Tests.Core;

public class EraserToolTests
{
    private readonly ToolOptions _options = new();
    private readonly PixelBuffer _layer = new(120, 60);

    public EraserToolTests() => _layer.Fill(RgbaColor.Black);

    [Fact]
    public void Kind_IsEraser() => Assert.Equal(ToolKind.Eraser, new EraserTool(_options).Kind);

    [Fact]
    public void Constructor_NullOptions_Throws() => Assert.Throws<ArgumentNullException>(() => new EraserTool(null!));

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void PointerDown_ClearsCircleOfSelectedSizeToTransparent(int size)
    {
        _options.SetSize(ToolKind.Eraser, size);

        var dirty = new EraserTool(_options).PointerDown(_layer, new CanvasPoint(60, 30));

        Assert.Equal(size, dirty.Width);
        Assert.Equal(RgbaColor.Transparent, _layer.GetPixel(60, 30));
        Assert.Equal(RgbaColor.Black, _layer.GetPixel(60 + (size / 2) + 1, 30));
    }

    [Fact]
    public void Drag_ErasesContinuousPath()
    {
        _options.SetSize(ToolKind.Eraser, 10);
        var eraser = new EraserTool(_options);

        eraser.PointerDown(_layer, new CanvasPoint(10, 30));
        eraser.PointerMove(new CanvasPoint(60, 30));
        eraser.PointerUp(new CanvasPoint(110, 30));

        Assert.All(Enumerable.Range(10, 100), x => Assert.Equal(0, _layer.GetPixel(x, 30).A));
        Assert.Equal(RgbaColor.Black, _layer.GetPixel(60, 10));
    }

    [Fact]
    public void ErasingDrawing_LeavesBackgroundLayerUntouched()
    {
        var document = new DrawingDocument();
        var background = document.Background.Pixels.GetBuffer().ToArray();
        var session = new DrawingSession(_options, [new PenTool(_options), new EraserTool(_options)], document);

        session.BeginStroke(new CanvasPoint(100, 100));
        session.EndStroke(new CanvasPoint(200, 100));
        _options.ActiveTool = ToolKind.Eraser;
        session.BeginStroke(new CanvasPoint(100, 100));
        session.EndStroke(new CanvasPoint(200, 100));

        Assert.All(Enumerable.Range(100, 101), x => Assert.Equal(RgbaColor.Transparent, document.ActiveLayer.Pixels.GetPixel(x, 100)));
        Assert.Equal(background, document.Background.Pixels.GetBuffer());
        Assert.Equal(RgbaColor.White, document.Background.Pixels.GetPixel(150, 100));
    }
}
