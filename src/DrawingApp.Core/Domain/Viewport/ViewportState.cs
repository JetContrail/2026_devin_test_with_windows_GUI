using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.Viewport;

/// <summary>
/// Maps canvas pixels to screen (view) coordinates: <c>screen = canvas * Scale + Offset</c>.
/// </summary>
public sealed class ViewportState
{
    /// <summary>Raised after <see cref="Scale"/> or the offset changed.</summary>
    public event EventHandler? Changed;

    public double Scale { get; private set; } = ZoomPolicy.DefaultScale;

    public double OffsetX { get; private set; }

    public double OffsetY { get; private set; }

    public CanvasPoint ScreenToCanvas(double screenX, double screenY) =>
        new((screenX - OffsetX) / Scale, (screenY - OffsetY) / Scale);

    /// <summary>
    /// Zooms by <paramref name="notches"/> steps of <see cref="ZoomPolicy"/>, keeping the canvas point under the
    /// given screen position fixed.
    /// </summary>
    public void ZoomAt(double screenX, double screenY, int notches)
    {
        var scale = ZoomPolicy.Step(Scale, notches);
        if (scale == Scale)
        {
            return;
        }

        var ratio = scale / Scale;
        OffsetX = screenX - ((screenX - OffsetX) * ratio);
        OffsetY = screenY - ((screenY - OffsetY) * ratio);
        Scale = scale;
        OnChanged();
    }

    public void PanBy(double dx, double dy)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        OffsetX += dx;
        OffsetY += dy;
        OnChanged();
    }

    /// <summary>Returns to 100% with the canvas origin at the top-left of the view.</summary>
    public void Reset()
    {
        Scale = ZoomPolicy.DefaultScale;
        OffsetX = 0;
        OffsetY = 0;
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
