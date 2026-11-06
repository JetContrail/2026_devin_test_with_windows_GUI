using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Application.Ports;

/// <summary>
/// Encodes a BGRA32 <see cref="PixelBuffer"/> into one file format. Register additional implementations
/// (e.g. WebP) to add save formats without touching callers.
/// </summary>
public interface IImageEncoder
{
    /// <summary>Short lowercase format id, e.g. <c>png</c>.</summary>
    string FormatId { get; }

    /// <summary>File extensions including the leading dot, e.g. <c>.png</c>.</summary>
    IReadOnlyList<string> Extensions { get; }

    void Encode(PixelBuffer image, Stream destination);
}
