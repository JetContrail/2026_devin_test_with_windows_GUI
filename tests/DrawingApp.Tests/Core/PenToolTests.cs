using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Tests.Core;

public class PenToolTests
{
    private readonly ToolOptions _options = new();
    private readonly PixelBuffer _buffer = new(100, 100);

    [Fact]
    public void Kind_IsPen() => Assert.Equal(ToolKind.Pen, new PenTool(_options).Kind);

    [Fact]
    public void Constructor_NullOptions_Throws() => Assert.Throws<ArgumentNullException>(() => new PenTool(null!));

    [Fact]
    public void Stroke_DrawsConnectedLineWithSelectedSizeAndColor()
    {
        _options.SetSize(ToolKind.Pen, 5);
        var pen = new PenTool(_options);

        var begin = pen.PointerDown(_buffer, new CanvasPoint(10.5, 50.5));
        var move = pen.PointerMove(new CanvasPoint(50.5, 50.5));
        var end = pen.PointerUp(new CanvasPoint(90.5, 50.5));

        Assert.Equal(new PixelRect(8, 48, 5, 5), begin);
        Assert.Equal(new PixelRect(8, 48, 45, 5), move);
        Assert.Equal(new PixelRect(48, 48, 45, 5), end);
        Assert.All(Enumerable.Range(10, 81), x => Assert.Equal(RgbaColor.Black, _buffer.GetPixel(x, 50)));
        Assert.Equal(0, _buffer.GetPixel(50, 45).A);
    }

    [Fact]
    public void Stroke_UsesColorFromOptions()
    {
        _options.Color = new RgbaColor(200, 0, 0);
        var pen = new PenTool(_options);

        pen.PointerDown(_buffer, new CanvasPoint(5, 5));

        Assert.Equal(new RgbaColor(200, 0, 0), _buffer.GetPixel(5, 5));
    }

    [Fact]
    public void SizeChangeDuringStroke_AppliesFromNextStroke()
    {
        _options.SetSize(ToolKind.Pen, 1);
        var pen = new PenTool(_options);

        pen.PointerDown(_buffer, new CanvasPoint(10.5, 10.5));
        _options.SetSize(ToolKind.Pen, 20);
        var dirty = pen.PointerUp(new CanvasPoint(20.5, 10.5));

        Assert.Equal(1, dirty.Height);
    }

    [Fact]
    public void MoveWithoutBegin_DoesNothing()
    {
        var pen = new PenTool(_options);

        Assert.True(pen.PointerMove(new CanvasPoint(5, 5)).IsEmpty);
        Assert.True(pen.PointerUp(new CanvasPoint(5, 5)).IsEmpty);
        Assert.Equal(0, _buffer.GetPixel(5, 5).A);
    }

    [Fact]
    public void MoveAfterEnd_DoesNothing()
    {
        var pen = new PenTool(_options);
        pen.PointerDown(_buffer, new CanvasPoint(5, 5));
        pen.PointerUp(new CanvasPoint(5, 5));

        Assert.True(pen.PointerMove(new CanvasPoint(50, 50)).IsEmpty);
    }
}
