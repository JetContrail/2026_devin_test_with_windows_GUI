using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.Layers;

/// <summary>Read-only underlay (the loaded sketch image). Never handed to tools.</summary>
public sealed class BackgroundLayer(PixelBuffer pixels) : Layer("背景", pixels)
{
    public override bool IsEditable => false;
}
