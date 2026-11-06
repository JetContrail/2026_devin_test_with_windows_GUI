using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Infrastructure.Imaging;

/// <summary>Writes 32-bit PNG with alpha, so untouched drawing pixels stay transparent.</summary>
public sealed class PngImageEncoder : IImageEncoder
{
    private const double Dpi = 96;

    public string FormatId => "png";

    public IReadOnlyList<string> Extensions { get; } = [".png"];

    public void Encode(PixelBuffer image, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(destination);

        var source = BitmapSource.Create(
            image.Width, image.Height, Dpi, Dpi, PixelFormats.Bgra32, null, image.GetBuffer(), image.Stride);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(destination);
    }
}
