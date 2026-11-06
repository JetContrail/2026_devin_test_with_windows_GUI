using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.Layers;

/// <summary>A named raster layer. Visibility and opacity are kept for future multi-layer support.</summary>
public abstract class Layer
{
    private double _opacity = 1.0;

    protected Layer(string name, PixelBuffer pixels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(pixels);

        Name = name;
        Pixels = pixels;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; }

    public bool IsVisible { get; set; } = true;

    /// <summary>Layer opacity in the range 0.0 (transparent) to 1.0 (opaque).</summary>
    public double Opacity
    {
        get => _opacity;
        set
        {
            if (double.IsNaN(value) || value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Opacity must be between 0 and 1.");
            }

            _opacity = value;
        }
    }

    /// <summary>Whether tools may modify <see cref="Pixels"/>.</summary>
    public abstract bool IsEditable { get; }

    public PixelBuffer Pixels { get; private set; }

    internal void ReplacePixels(PixelBuffer pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        Pixels = pixels;
    }
}
