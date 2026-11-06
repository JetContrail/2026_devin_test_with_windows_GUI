using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.Rasterization;

/// <summary>Hard-edged (non anti-aliased) round brush. All methods clip to the target and return the changed area.</summary>
public static class RoundBrushRasterizer
{
    public static PixelRect Stamp(PixelBuffer target, CanvasPoint center, int diameter, RgbaColor color, BlendMode mode)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(diameter);

        // Odd sizes are centered on a pixel and even sizes on a pixel corner, so an n px brush covers exactly n pixels across.
        var (cx, cy) = diameter % 2 == 1
            ? (Math.Floor(center.X) + 0.5, Math.Floor(center.Y) + 0.5)
            : (Math.Round(center.X), Math.Round(center.Y));
        var radius = diameter / 2.0;
        var radiusSquared = radius * radius;

        var area = PixelRect.FromBounds(
            (int)Math.Floor(cx - radius),
            (int)Math.Floor(cy - radius),
            (int)Math.Ceiling(cx + radius),
            (int)Math.Ceiling(cy + radius)).Intersect(target.Bounds);
        var pixel = mode == BlendMode.Erase ? RgbaColor.Transparent : color;

        for (var y = area.Y; y < area.Bottom; y++)
        {
            var dy = y + 0.5 - cy;
            for (var x = area.X; x < area.Right; x++)
            {
                var dx = x + 0.5 - cx;
                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    target.SetPixel(x, y, pixel);
                }
            }
        }

        return area;
    }

    /// <summary>Stamps along the segment densely enough that fast mouse movement leaves no gaps.</summary>
    public static PixelRect Line(PixelBuffer target, CanvasPoint from, CanvasPoint to, int diameter, RgbaColor color, BlendMode mode)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(diameter);

        var spacing = Math.Max(1.0, diameter / 4.0);
        var steps = Math.Max(1, (int)Math.Ceiling(from.DistanceTo(to) / spacing));
        var dirty = PixelRect.Empty;

        for (var i = 0; i <= steps; i++)
        {
            var t = (double)i / steps;
            var point = new CanvasPoint(from.X + ((to.X - from.X) * t), from.Y + ((to.Y - from.Y) * t));
            dirty = dirty.Union(Stamp(target, point, diameter, color, mode));
        }

        return dirty;
    }
}
