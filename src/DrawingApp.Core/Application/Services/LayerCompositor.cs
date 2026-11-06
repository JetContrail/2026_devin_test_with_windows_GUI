using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Layers;

namespace DrawingApp.Core.Application.Services;

/// <summary>Flattens visible layers bottom to top with source-over blending, honouring each layer's opacity.</summary>
public static class LayerCompositor
{
    public static PixelBuffer Compose(LayerStack layers)
    {
        ArgumentNullException.ThrowIfNull(layers);

        var size = layers.Background.Pixels;
        var result = new PixelBuffer(size.Width, size.Height);
        foreach (var layer in layers.Layers)
        {
            if (layer.IsVisible && layer.Opacity > 0)
            {
                BlendOver(result, layer.Pixels, layer.Opacity);
            }
        }

        return result;
    }

    // Straight (non-premultiplied) alpha: outA = sA + dA(1 - sA), outC = (sC·sA + dC·dA(1 - sA)) / outA.
    private static void BlendOver(PixelBuffer destination, PixelBuffer source, double opacity)
    {
        var dst = destination.GetBuffer();
        var src = source.GetBuffer();
        var length = Math.Min(dst.Length, src.Length);
        for (var i = 0; i < length; i += PixelBuffer.BytesPerPixel)
        {
            var sa = src[i + 3] / 255.0 * opacity;
            if (sa <= 0)
            {
                continue;
            }

            var da = dst[i + 3] / 255.0;
            var keep = da * (1 - sa);
            var oa = sa + keep;
            for (var c = 0; c < 3; c++)
            {
                dst[i + c] = (byte)Math.Round(((src[i + c] * sa) + (dst[i + c] * keep)) / oa);
            }

            dst[i + 3] = (byte)Math.Round(oa * 255);
        }
    }
}
