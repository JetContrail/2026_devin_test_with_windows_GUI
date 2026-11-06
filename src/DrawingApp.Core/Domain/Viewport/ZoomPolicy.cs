namespace DrawingApp.Core.Domain.Viewport;

/// <summary>Allowed zoom factors (1.0 = 100%). One mouse-wheel notch moves one step through <see cref="Steps"/>.</summary>
public static class ZoomPolicy
{
    public const double DefaultScale = 1.0;

    private const double Tolerance = 1e-9;

    public static IReadOnlyList<double> Steps { get; } =
        [0.10, 0.15, 0.20, 0.25, 0.33, 0.50, 0.67, 0.75, 1.00, 1.25, 1.50, 2.00, 3.00, 4.00, 6.00, 8.00];

    public static double MinScale => Steps[0];

    public static double MaxScale => Steps[^1];

    public static double Clamp(double scale) => Math.Clamp(scale, MinScale, MaxScale);

    /// <summary>
    /// Returns the scale <paramref name="notches"/> steps above (positive) or below (negative) <paramref name="current"/>,
    /// clamped to the table. A scale between two steps snaps to the next step in the requested direction.
    /// </summary>
    public static double Step(double current, int notches)
    {
        if (notches == 0)
        {
            return Clamp(current);
        }

        int index;
        if (notches > 0)
        {
            index = IndexOfFirst(s => s > current + Tolerance, Steps.Count - 1) + notches - 1;
        }
        else
        {
            index = IndexOfLast(s => s < current - Tolerance, 0) + notches + 1;
        }

        return Steps[Math.Clamp(index, 0, Steps.Count - 1)];
    }

    private static int IndexOfFirst(Func<double, bool> predicate, int fallback)
    {
        for (var i = 0; i < Steps.Count; i++)
        {
            if (predicate(Steps[i]))
            {
                return i;
            }
        }

        return fallback;
    }

    private static int IndexOfLast(Func<double, bool> predicate, int fallback)
    {
        for (var i = Steps.Count - 1; i >= 0; i--)
        {
            if (predicate(Steps[i]))
            {
                return i;
            }
        }

        return fallback;
    }
}
