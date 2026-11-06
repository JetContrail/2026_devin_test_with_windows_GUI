using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.UI.Rendering;

/// <summary>Mirrors a <see cref="PixelBuffer"/> into a <see cref="WriteableBitmap"/>, copying only dirty regions.</summary>
public sealed class LayerBitmapPresenter
{
    private const double Dpi = 96;

    private readonly PixelBuffer _source;

    public LayerBitmapPresenter(PixelBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        Bitmap = new WriteableBitmap(source.Width, source.Height, Dpi, Dpi, PixelFormats.Bgra32, null);
        Update(source.Bounds);
    }

    public WriteableBitmap Bitmap { get; }

    public void Update(PixelRect area)
    {
        var clipped = area.Intersect(_source.Bounds);
        if (clipped.IsEmpty)
        {
            return;
        }

        Bitmap.WritePixels(
            new Int32Rect(clipped.X, clipped.Y, clipped.Width, clipped.Height),
            _source.GetBuffer(),
            _source.Stride,
            clipped.X,
            clipped.Y);
    }
}
