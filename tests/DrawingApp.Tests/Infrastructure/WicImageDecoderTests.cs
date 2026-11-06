using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Infrastructure.Imaging;
using DrawingApp.Tests.UI;

namespace DrawingApp.Tests.Infrastructure;

public class WicImageDecoderTests
{
    [Fact]
    public void Decode_Png_ReturnsBgra32PixelsWithAlpha() => StaThread.Run(() =>
    {
        // 3x2 image: top-left red, bottom-right half-transparent blue (BGRA byte order).
        var pixels = new byte[3 * 2 * 4];
        new byte[] { 0, 0, 255, 255 }.CopyTo(pixels, 0);
        new byte[] { 255, 0, 0, 128 }.CopyTo(pixels, 20);

        var buffer = new WicImageDecoder().Decode(Encode(new PngBitmapEncoder(), 3, 2, pixels, dpi: 300));

        Assert.Equal((3, 2), (buffer.Width, buffer.Height));
        Assert.Equal(new RgbaColor(255, 0, 0), buffer.GetPixel(0, 0));
        Assert.Equal(new RgbaColor(0, 0, 255, 128), buffer.GetPixel(2, 1));
        Assert.Equal(RgbaColor.Transparent, buffer.GetPixel(1, 0));
    });

    [Fact]
    public void Decode_Jpeg_ReturnsOpaquePixels() => StaThread.Run(() =>
    {
        var pixels = Enumerable.Repeat(new byte[] { 40, 80, 160, 255 }, 16 * 8).SelectMany(p => p).ToArray();

        var buffer = new WicImageDecoder().Decode(Encode(new JpegBitmapEncoder { QualityLevel = 100 }, 16, 8, pixels));

        Assert.Equal((16, 8), (buffer.Width, buffer.Height));
        var color = buffer.GetPixel(8, 4);
        Assert.Equal(255, color.A);
        Assert.InRange(color.R, 150, 170);
        Assert.InRange(color.B, 30, 50);
    });

    [Fact]
    public void Decode_InvalidData_ThrowsImageDecodeException() => StaThread.Run(() =>
        Assert.Throws<ImageDecodeException>(() => new WicImageDecoder().Decode(new MemoryStream([1, 2, 3, 4, 5]))));

    [Fact]
    public void Decode_TooLarge_ThrowsImageDecodeException() => StaThread.Run(() =>
    {
        var stream = Encode(new PngBitmapEncoder(), 4, 4, new byte[4 * 4 * 4]);

        var ex = Assert.Throws<ImageDecodeException>(() => new WicImageDecoder(maxPixelCount: 15).Decode(stream));

        Assert.Contains("4x4", ex.Message, StringComparison.Ordinal);
    });

    [Fact]
    public void ArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new WicImageDecoder().Decode(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WicImageDecoder(0));
        Assert.Equal(4096L * 4096L, WicImageDecoder.DefaultMaxPixelCount);
    }

    private static MemoryStream Encode(BitmapEncoder encoder, int width, int height, byte[] bgra, double dpi = 96)
    {
        var source = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgra32, null, bgra, width * 4);
        encoder.Frames.Add(BitmapFrame.Create(source));
        var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
