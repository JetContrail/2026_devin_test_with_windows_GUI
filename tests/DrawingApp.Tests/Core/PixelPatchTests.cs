using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public class PixelPatchTests
{
    private static readonly RgbaColor Red = new(255, 0, 0, 255);

    [Fact]
    public void ApplyTo_RestoresOnlyCapturedArea()
    {
        var buffer = new PixelBuffer(4, 4);
        buffer.SetPixel(1, 1, Red);
        var patch = PixelPatch.Capture(buffer, new PixelRect(1, 1, 2, 2));
        buffer.Fill(RgbaColor.Black);

        patch.ApplyTo(buffer);

        Assert.Equal(Red, buffer.GetPixel(1, 1));
        Assert.Equal(RgbaColor.Transparent, buffer.GetPixel(2, 2));
        Assert.Equal(RgbaColor.Black, buffer.GetPixel(0, 0));
        Assert.Equal(RgbaColor.Black, buffer.GetPixel(3, 3));
    }

    [Fact]
    public void Capture_ClipsAreaToBuffer()
    {
        var patch = PixelPatch.Capture(new PixelBuffer(4, 4), new PixelRect(-2, 2, 10, 10));

        Assert.Equal(new PixelRect(0, 2, 4, 2), patch.Area);
    }

    [Fact]
    public void Capture_OutsideBuffer_IsEmptyAndApplyIsNoOp()
    {
        var buffer = new PixelBuffer(2, 2);
        var patch = PixelPatch.Capture(buffer, new PixelRect(5, 5, 3, 3));

        patch.ApplyTo(buffer);

        Assert.True(patch.Area.IsEmpty);
    }

    [Fact]
    public void Capture_UniformArea_IsCompressed()
    {
        var patch = PixelPatch.Capture(new PixelBuffer(256, 256), new PixelRect(0, 0, 256, 256));

        Assert.True(patch.SizeInBytes < 256 * 256 * PixelBuffer.BytesPerPixel / 100);
    }

    [Fact]
    public void ApplyTo_AreaOutsideTarget_Throws()
    {
        var patch = PixelPatch.Capture(new PixelBuffer(4, 4), new PixelRect(2, 2, 2, 2));

        Assert.Throws<ArgumentException>(() => patch.ApplyTo(new PixelBuffer(3, 3)));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var patch = PixelPatch.Capture(new PixelBuffer(1, 1), new PixelRect(0, 0, 1, 1));

        Assert.Throws<ArgumentNullException>(() => PixelPatch.Capture(null!, PixelRect.Empty));
        Assert.Throws<ArgumentNullException>(() => patch.ApplyTo(null!));
    }
}
