using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public class PixelBufferTests
{
    [Fact]
    public void New_IsTransparentWithBgra32Layout()
    {
        var buffer = new PixelBuffer(3, 2);

        Assert.Equal(12, buffer.Stride);
        Assert.Equal(24, buffer.GetBuffer().Length);
        Assert.Equal(new PixelRect(0, 0, 3, 2), buffer.Bounds);
        Assert.All(buffer.GetBuffer(), b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void New_InvalidSize_Throws(int width, int height) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(width, height));

    [Fact]
    public void SetPixel_WritesBgraBytesAndRoundTrips()
    {
        var buffer = new PixelBuffer(2, 2);
        var color = new RgbaColor(10, 20, 30, 40);

        buffer.SetPixel(1, 1, color);

        Assert.Equal(color, buffer.GetPixel(1, 1));
        Assert.Equal(new byte[] { 30, 20, 10, 40 }, buffer.GetBuffer()[12..16]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    public void GetPixel_OutOfRange_Throws(int x, int y)
    {
        var buffer = new PixelBuffer(2, 2);

        Assert.False(buffer.Contains(x, y));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(x, y));
    }

    [Fact]
    public void Fill_SetsEveryPixel()
    {
        var buffer = new PixelBuffer(3, 3);

        buffer.Fill(RgbaColor.White);

        Assert.All(buffer.GetBuffer(), b => Assert.Equal(255, b));
    }
}
