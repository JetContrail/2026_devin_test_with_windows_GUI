using System.IO.Compression;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.History;

/// <summary>Deflate-compressed copy of a rectangular region of a <see cref="PixelBuffer"/>.</summary>
public sealed class PixelPatch
{
    private readonly byte[] _compressed;

    private PixelPatch(PixelRect area, byte[] compressed)
    {
        Area = area;
        _compressed = compressed;
    }

    /// <summary>Captured region, clipped to the source bounds.</summary>
    public PixelRect Area { get; }

    public int SizeInBytes => _compressed.Length;

    public static PixelPatch Capture(PixelBuffer source, PixelRect area)
    {
        ArgumentNullException.ThrowIfNull(source);

        var clipped = area.Intersect(source.Bounds);
        var pixels = source.GetBuffer();
        var rowBytes = clipped.Width * PixelBuffer.BytesPerPixel;
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (var y = clipped.Y; y < clipped.Bottom; y++)
            {
                deflate.Write(pixels, OffsetOf(source, clipped.X, y), rowBytes);
            }
        }

        return new PixelPatch(clipped, output.ToArray());
    }

    /// <summary>Writes the captured pixels back to the same region of <paramref name="target"/>.</summary>
    public void ApplyTo(PixelBuffer target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.Bounds.Intersect(Area) != Area)
        {
            throw new ArgumentException($"Patch {Area} does not fit a {target.Width}x{target.Height} buffer.", nameof(target));
        }

        var pixels = target.GetBuffer();
        var rowBytes = Area.Width * PixelBuffer.BytesPerPixel;
        using var deflate = new DeflateStream(new MemoryStream(_compressed), CompressionMode.Decompress);
        for (var y = Area.Y; y < Area.Bottom; y++)
        {
            deflate.ReadExactly(pixels, OffsetOf(target, Area.X, y), rowBytes);
        }
    }

    private static int OffsetOf(PixelBuffer buffer, int x, int y) => (y * buffer.Stride) + (x * PixelBuffer.BytesPerPixel);
}
