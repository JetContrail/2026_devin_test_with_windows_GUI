using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public class DrawingDocumentTests
{
    [Fact]
    public void New_IsDefaultSizeWithWhiteBackgroundAndTransparentDrawing()
    {
        var document = new DrawingDocument();

        Assert.Equal((1280, 720), (document.Width, document.Height));
        Assert.Equal(RgbaColor.White, document.Background.Pixels.GetPixel(640, 360));
        Assert.Equal(RgbaColor.Transparent, document.ActiveLayer.Pixels.GetPixel(640, 360));
        Assert.Equal((1280, 720), (document.ActiveLayer.Pixels.Width, document.ActiveLayer.Pixels.Height));
        Assert.Equal(2, document.Layers.Layers.Count);
    }

    [Fact]
    public void LayersAreIndependentBuffers()
    {
        var document = new DrawingDocument(4, 4);

        document.ActiveLayer.Pixels.SetPixel(1, 1, RgbaColor.Black);

        Assert.NotSame(document.Background.Pixels, document.ActiveLayer.Pixels);
        Assert.Equal(RgbaColor.White, document.Background.Pixels.GetPixel(1, 1));
    }

    [Fact]
    public void LoadBackground_ResizesCanvasKeepsDrawingAtTopLeftAndRaisesReset()
    {
        var document = new DrawingDocument(10, 10);
        var drawing = document.ActiveLayer;
        drawing.Pixels.SetPixel(2, 2, RgbaColor.Black);
        drawing.Pixels.SetPixel(9, 9, RgbaColor.Black);
        var image = new PixelBuffer(6, 20);
        var resets = 0;
        document.Reset += (_, _) => resets++;

        document.LoadBackground(image);

        Assert.Equal(1, resets);
        Assert.Equal((6, 20), (document.Width, document.Height));
        Assert.Same(image, document.Background.Pixels);
        Assert.Same(drawing, document.ActiveLayer);
        Assert.Equal((6, 20), (drawing.Pixels.Width, drawing.Pixels.Height));
        Assert.Equal(RgbaColor.Black, drawing.Pixels.GetPixel(2, 2));
        Assert.Equal(RgbaColor.Transparent, drawing.Pixels.GetPixel(5, 9));
    }

    [Fact]
    public void LoadBackground_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new DrawingDocument().LoadBackground(null!));
}
