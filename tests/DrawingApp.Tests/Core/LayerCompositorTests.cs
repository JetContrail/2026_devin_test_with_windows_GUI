using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public class LayerCompositorTests
{
    private readonly DrawingDocument _document = new(4, 2);

    [Fact]
    public void Compose_DrawsDrawingOverBackground()
    {
        _document.ActiveLayer.Pixels.SetPixel(1, 0, RgbaColor.Black);

        var result = LayerCompositor.Compose(_document.Layers);

        Assert.Equal((4, 2), (result.Width, result.Height));
        Assert.Equal(RgbaColor.Black, result.GetPixel(1, 0));
        Assert.Equal(RgbaColor.White, result.GetPixel(0, 0));
    }

    [Fact]
    public void Compose_DoesNotModifyLayers()
    {
        _document.ActiveLayer.Pixels.SetPixel(1, 0, RgbaColor.Black);
        var background = _document.Background.Pixels.GetBuffer().ToArray();
        var drawing = _document.ActiveLayer.Pixels.GetBuffer().ToArray();

        LayerCompositor.Compose(_document.Layers);

        Assert.Equal(background, _document.Background.Pixels.GetBuffer());
        Assert.Equal(drawing, _document.ActiveLayer.Pixels.GetBuffer());
    }

    [Fact]
    public void Compose_BlendsSemiTransparentPixels()
    {
        _document.ActiveLayer.Pixels.SetPixel(0, 0, new RgbaColor(0, 0, 0, 128));

        var result = LayerCompositor.Compose(_document.Layers);

        Assert.Equal(new RgbaColor(127, 127, 127), result.GetPixel(0, 0));
    }

    [Fact]
    public void Compose_AppliesLayerOpacity()
    {
        _document.ActiveLayer.Pixels.SetPixel(0, 0, RgbaColor.Black);
        _document.ActiveLayer.Opacity = 0.5;

        var result = LayerCompositor.Compose(_document.Layers);

        Assert.Equal(new RgbaColor(128, 128, 128), result.GetPixel(0, 0));
    }

    [Fact]
    public void Compose_SkipsHiddenLayers()
    {
        _document.ActiveLayer.Pixels.SetPixel(0, 0, RgbaColor.Black);
        _document.ActiveLayer.IsVisible = false;
        _document.Background.IsVisible = false;

        var result = LayerCompositor.Compose(_document.Layers);

        Assert.All(Enumerable.Range(0, 4), x => Assert.Equal(RgbaColor.Transparent, result.GetPixel(x, 0)));
    }

    [Fact]
    public void Compose_TransparentBackground_KeepsDrawingColorAndAlpha()
    {
        _document.Background.IsVisible = false;
        _document.ActiveLayer.Pixels.SetPixel(2, 1, new RgbaColor(200, 100, 50, 60));

        var result = LayerCompositor.Compose(_document.Layers);

        Assert.Equal(new RgbaColor(200, 100, 50, 60), result.GetPixel(2, 1));
    }

    [Fact]
    public void Compose_Null_Throws() => Assert.Throws<ArgumentNullException>(() => LayerCompositor.Compose(null!));
}
