using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Tests.Core;

public class DotToolTests
{
    private readonly ToolOptions _options = new();
    private readonly PixelBuffer _buffer = new(64, 64);

    [Fact]
    public void Kind_IsDot() => Assert.Equal(ToolKind.Dot, new DotTool(_options).Kind);

    [Fact]
    public void Constructor_NullOptions_Throws() => Assert.Throws<ArgumentNullException>(() => new DotTool(null!));

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void PointerDown_PlacesDotWithSelectedDiameterCenteredOnClick(int size)
    {
        _options.SetSize(ToolKind.Dot, size);

        var dirty = new DotTool(_options).PointerDown(_buffer, new CanvasPoint(32, 32));

        Assert.Equal(size, dirty.Width);
        Assert.Equal(size, dirty.Height);
        Assert.Equal(RgbaColor.Black, _buffer.GetPixel(32, 32));
    }

    [Fact]
    public void Drag_PlacesOnlyOneDot()
    {
        var dot = new DotTool(_options);

        dot.PointerDown(_buffer, new CanvasPoint(10.5, 10.5));
        var move = dot.PointerMove(new CanvasPoint(40.5, 10.5));
        var up = dot.PointerUp(new CanvasPoint(50.5, 10.5));

        Assert.True(move.IsEmpty);
        Assert.True(up.IsEmpty);
        Assert.Equal(0, _buffer.GetPixel(40, 10).A);
        Assert.Equal(0, _buffer.GetPixel(50, 10).A);
    }

    [Fact]
    public void PointerDown_UsesColorFromOptions()
    {
        _options.Color = new RgbaColor(0, 128, 0);

        new DotTool(_options).PointerDown(_buffer, new CanvasPoint(5, 5));

        Assert.Equal(new RgbaColor(0, 128, 0), _buffer.GetPixel(5, 5));
    }

    [Fact]
    public void PointerDown_NullTarget_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DotTool(_options).PointerDown(null!, default));
}
