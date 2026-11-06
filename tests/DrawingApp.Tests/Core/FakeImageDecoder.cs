using System.IO;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

/// <summary>Returns an image of the configured size, or throws <see cref="ImageDecodeException"/> when <see cref="Fail"/> is set.</summary>
internal sealed class FakeImageDecoder(int width = 64, int height = 32) : IImageDecoder
{
    public bool Fail { get; set; }

    public int DecodeCount { get; private set; }

    public PixelBuffer Decode(Stream stream)
    {
        DecodeCount++;
        if (Fail)
        {
            throw new ImageDecodeException("broken");
        }

        var image = new PixelBuffer(width, height);
        image.Fill(new RgbaColor(10, 20, 30));
        return image;
    }
}
