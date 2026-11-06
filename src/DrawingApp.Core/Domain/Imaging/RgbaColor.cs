namespace DrawingApp.Core.Domain.Imaging;

/// <summary>Non-premultiplied 8-bit RGBA color.</summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255)
{
    public static RgbaColor Black { get; } = new(0, 0, 0);

    public static RgbaColor White { get; } = new(255, 255, 255);

    public static RgbaColor Transparent { get; } = new(0, 0, 0, 0);
}
