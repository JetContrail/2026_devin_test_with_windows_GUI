using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Rasterization;

namespace DrawingApp.Tests.Core;

public class RoundBrushRasterizerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void Stamp_CoversExactlyDiameterPixelsAcross(int diameter)
    {
        var buffer = new PixelBuffer(64, 64);

        var dirty = RoundBrushRasterizer.Stamp(buffer, new CanvasPoint(32.2, 32.2), diameter, RgbaColor.Black, BlendMode.Paint);

        var row = Enumerable.Range(0, 64).Count(x => buffer.GetPixel(x, (int)(dirty.Y + (dirty.Height / 2))).A != 0);
        Assert.Equal(diameter, row);
        Assert.Equal(diameter, dirty.Width);
        Assert.Equal(diameter, dirty.Height);
    }

    [Fact]
    public void Stamp_DiameterOne_PaintsTheContainingPixel()
    {
        var buffer = new PixelBuffer(4, 4);

        RoundBrushRasterizer.Stamp(buffer, new CanvasPoint(2.0, 1.0), 1, RgbaColor.Black, BlendMode.Paint);

        Assert.Equal(RgbaColor.Black, buffer.GetPixel(2, 1));
        Assert.Equal(1, CountOpaque(buffer));
    }

    [Fact]
    public void Stamp_IsRound()
    {
        var buffer = new PixelBuffer(32, 32);

        RoundBrushRasterizer.Stamp(buffer, new CanvasPoint(16, 16), 10, RgbaColor.Black, BlendMode.Paint);

        Assert.Equal(0, buffer.GetPixel(11, 11).A);
        Assert.Equal(255, buffer.GetPixel(16, 16).A);
    }

    [Fact]
    public void Stamp_NearEdge_ClipsToBuffer()
    {
        var buffer = new PixelBuffer(8, 8);

        var dirty = RoundBrushRasterizer.Stamp(buffer, new CanvasPoint(0, 0), 10, RgbaColor.Black, BlendMode.Paint);

        Assert.Equal(new PixelRect(0, 0, 5, 5), dirty);
    }

    [Fact]
    public void Stamp_OutsideBuffer_ReturnsEmpty()
    {
        var buffer = new PixelBuffer(8, 8);

        var dirty = RoundBrushRasterizer.Stamp(buffer, new CanvasPoint(-50, -50), 5, RgbaColor.Black, BlendMode.Paint);

        Assert.True(dirty.IsEmpty);
        Assert.Equal(0, CountOpaque(buffer));
    }

    [Fact]
    public void Stamp_Erase_MakesPixelsTransparent()
    {
        var buffer = new PixelBuffer(8, 8);
        buffer.Fill(RgbaColor.Black);

        RoundBrushRasterizer.Stamp(buffer, new CanvasPoint(4.5, 4.5), 1, RgbaColor.Black, BlendMode.Erase);

        Assert.Equal(RgbaColor.Transparent, buffer.GetPixel(4, 4));
        Assert.Equal(63, CountOpaque(buffer));
    }

    [Fact]
    public void Stamp_InvalidDiameter_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RoundBrushRasterizer.Stamp(new PixelBuffer(1, 1), default, 0, RgbaColor.Black, BlendMode.Paint));

    [Fact]
    public void Line_LongFastSegment_HasNoGaps()
    {
        var buffer = new PixelBuffer(200, 10);

        var dirty = RoundBrushRasterizer.Line(buffer, new CanvasPoint(5.5, 5.5), new CanvasPoint(195.5, 5.5), 1, RgbaColor.Black, BlendMode.Paint);

        Assert.All(Enumerable.Range(5, 191), x => Assert.Equal(255, buffer.GetPixel(x, 5).A));
        Assert.Equal(new PixelRect(5, 5, 191, 1), dirty);
    }

    [Fact]
    public void Line_Diagonal_IsConnected()
    {
        var buffer = new PixelBuffer(50, 50);

        RoundBrushRasterizer.Line(buffer, new CanvasPoint(0.5, 0.5), new CanvasPoint(49.5, 49.5), 1, RgbaColor.Black, BlendMode.Paint);

        Assert.All(Enumerable.Range(0, 50), i => Assert.Equal(255, buffer.GetPixel(i, i).A));
    }

    [Fact]
    public void Line_ZeroLength_StampsOnce()
    {
        var buffer = new PixelBuffer(10, 10);

        var dirty = RoundBrushRasterizer.Line(buffer, new CanvasPoint(5, 5), new CanvasPoint(5, 5), 3, RgbaColor.Black, BlendMode.Paint);

        Assert.Equal(new PixelRect(4, 4, 3, 3), dirty);
    }

    private static int CountOpaque(PixelBuffer buffer)
    {
        var count = 0;
        for (var y = 0; y < buffer.Height; y++)
        {
            for (var x = 0; x < buffer.Width; x++)
            {
                if (buffer.GetPixel(x, y).A != 0)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
