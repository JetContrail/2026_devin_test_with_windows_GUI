using System.IO;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Infrastructure.Imaging;
using DrawingApp.Tests.UI;

namespace DrawingApp.Tests.Infrastructure;

public class PngImageEncoderTests
{
    private readonly PngImageEncoder _encoder = new();

    [Fact]
    public void Metadata_DescribesPng()
    {
        Assert.Equal("png", _encoder.FormatId);
        Assert.Equal([".png"], _encoder.Extensions);
    }

    [Fact]
    public void Encode_ThenDecode_RoundTripsPixelsIncludingAlpha() => StaThread.Run(() =>
    {
        var image = new PixelBuffer(4, 3);
        image.SetPixel(0, 0, RgbaColor.Black);
        image.SetPixel(1, 0, new RgbaColor(255, 0, 0));
        image.SetPixel(3, 2, new RgbaColor(0, 128, 255, 77));
        using var stream = new MemoryStream();

        _encoder.Encode(image, stream);
        stream.Position = 0;
        var decoded = new WicImageDecoder().Decode(stream);

        Assert.Equal(image.GetBuffer(), decoded.GetBuffer());
        Assert.Equal(RgbaColor.Transparent, decoded.GetPixel(2, 1));
    });

    [Fact]
    public void Encode_WritesPngSignature() => StaThread.Run(() =>
    {
        using var stream = new MemoryStream();

        _encoder.Encode(new PixelBuffer(1, 1), stream);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, stream.ToArray()[..4]);
    });

    [Fact]
    public void Encode_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => _encoder.Encode(null!, new MemoryStream()));
        Assert.Throws<ArgumentNullException>(() => _encoder.Encode(new PixelBuffer(1, 1), null!));
    }
}
