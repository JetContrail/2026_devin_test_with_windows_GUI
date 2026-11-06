using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Layers;

namespace DrawingApp.Tests.Core;

public class LayerTests
{
    [Fact]
    public void BackgroundLayer_IsNotEditable()
    {
        var layer = new BackgroundLayer(new PixelBuffer(2, 2));

        Assert.False(layer.IsEditable);
        Assert.Equal("背景", layer.Name);
    }

    [Fact]
    public void RasterLayer_IsEditableVisibleAndOpaqueByDefault()
    {
        var pixels = new PixelBuffer(2, 2);
        var layer = new RasterLayer("描画", pixels);

        Assert.True(layer.IsEditable);
        Assert.True(layer.IsVisible);
        Assert.Equal(1.0, layer.Opacity);
        Assert.Same(pixels, layer.Pixels);
        Assert.NotEqual(Guid.Empty, layer.Id);
    }

    [Fact]
    public void VisibilityAndOpacity_CanBeChanged()
    {
        var layer = new RasterLayer("a", new PixelBuffer(1, 1)) { IsVisible = false, Opacity = 0.25 };

        Assert.False(layer.IsVisible);
        Assert.Equal(0.25, layer.Opacity);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Opacity_OutOfRange_Throws(double opacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RasterLayer("a", new PixelBuffer(1, 1)).Opacity = opacity);

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentException>(() => new RasterLayer(" ", new PixelBuffer(1, 1)));
        Assert.Throws<ArgumentNullException>(() => new RasterLayer("a", null!));
        Assert.Throws<ArgumentNullException>(() => new BackgroundLayer(null!));
    }

    [Fact]
    public void LayerStack_OrdersBackgroundBelowDrawing()
    {
        var background = new BackgroundLayer(new PixelBuffer(1, 1));
        var drawing = new RasterLayer("描画", new PixelBuffer(1, 1));

        var stack = new LayerStack(background, drawing);

        Assert.Equal(new Layer[] { background, drawing }, stack.Layers);
        Assert.Same(background, stack.Background);
        Assert.Same(drawing, stack.ActiveLayer);
        Assert.Throws<ArgumentNullException>(() => new LayerStack(null!, drawing));
        Assert.Throws<ArgumentNullException>(() => new LayerStack(background, null!));
    }
}
