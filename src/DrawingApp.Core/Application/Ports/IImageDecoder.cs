using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Application.Ports;

/// <summary>Decodes an encoded image (PNG/JPEG) into a BGRA32 <see cref="PixelBuffer"/> at its pixel size.</summary>
public interface IImageDecoder
{
    /// <exception cref="ImageDecodeException">The data is not a supported image or is too large.</exception>
    PixelBuffer Decode(Stream stream);
}
