using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.Layers;

/// <summary>Editable, initially transparent layer that receives the user's drawing.</summary>
public sealed class RasterLayer(string name, PixelBuffer pixels) : Layer(name, pixels)
{
    public override bool IsEditable => true;
}
