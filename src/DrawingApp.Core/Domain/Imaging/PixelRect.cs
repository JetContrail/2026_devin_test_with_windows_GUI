namespace DrawingApp.Core.Domain.Imaging;

/// <summary>Integer pixel rectangle. <see cref="Right"/> and <see cref="Bottom"/> are exclusive.</summary>
public readonly record struct PixelRect
{
    public PixelRect(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static PixelRect Empty { get; }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool IsEmpty => Width == 0 || Height == 0;

    public static PixelRect FromBounds(int left, int top, int right, int bottom) =>
        right <= left || bottom <= top ? Empty : new PixelRect(left, top, right - left, bottom - top);

    public PixelRect Union(PixelRect other)
    {
        if (IsEmpty)
        {
            return other;
        }

        if (other.IsEmpty)
        {
            return this;
        }

        return FromBounds(Math.Min(X, other.X), Math.Min(Y, other.Y), Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));
    }

    public PixelRect Intersect(PixelRect other) =>
        FromBounds(Math.Max(X, other.X), Math.Max(Y, other.Y), Math.Min(Right, other.Right), Math.Min(Bottom, other.Bottom));
}
