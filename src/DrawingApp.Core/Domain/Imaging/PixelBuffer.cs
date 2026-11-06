namespace DrawingApp.Core.Domain.Imaging;

/// <summary>
/// Raster image stored as non-premultiplied BGRA32 (the byte layout of WPF's <c>PixelFormats.Bgra32</c>),
/// so the UI can copy regions into a bitmap without conversion.
/// </summary>
public sealed class PixelBuffer
{
    public const int BytesPerPixel = 4;

    private readonly byte[] _pixels;

    public PixelBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        _pixels = new byte[checked(width * height * BytesPerPixel)];
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride => Width * BytesPerPixel;

    public PixelRect Bounds => new(0, 0, Width, Height);

    /// <summary>Returns the backing array for zero-copy interop (e.g. <c>WriteableBitmap.WritePixels</c>).</summary>
    public byte[] GetBuffer() => _pixels;

    public bool Contains(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    public RgbaColor GetPixel(int x, int y)
    {
        var offset = OffsetOf(x, y);
        return new RgbaColor(_pixels[offset + 2], _pixels[offset + 1], _pixels[offset], _pixels[offset + 3]);
    }

    public void SetPixel(int x, int y, RgbaColor color)
    {
        var offset = OffsetOf(x, y);
        _pixels[offset] = color.B;
        _pixels[offset + 1] = color.G;
        _pixels[offset + 2] = color.R;
        _pixels[offset + 3] = color.A;
    }

    public void Fill(RgbaColor color)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                SetPixel(x, y, color);
            }
        }
    }

    /// <summary>Returns a new buffer of the given size with this content copied to its top-left corner (cropped if larger).</summary>
    public PixelBuffer Resized(int width, int height)
    {
        var result = new PixelBuffer(width, height);
        var rowBytes = Math.Min(Width, width) * BytesPerPixel;
        for (var y = 0; y < Math.Min(Height, height); y++)
        {
            Buffer.BlockCopy(_pixels, y * Stride, result._pixels, y * result.Stride, rowBytes);
        }

        return result;
    }

    private int OffsetOf(int x, int y)
    {
        if (!Contains(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"({x}, {y}) is outside the {Width}x{Height} buffer.");
        }

        return (y * Stride) + (x * BytesPerPixel);
    }
}
