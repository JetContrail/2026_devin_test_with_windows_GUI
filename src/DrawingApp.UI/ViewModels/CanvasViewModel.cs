using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.UI.ViewModels;

/// <summary>
/// Exposes the drawing layer to the canvas view and forwards pointer input (in canvas pixels) to the <see cref="DrawingSession"/>.
/// </summary>
public sealed class CanvasViewModel : IDisposable
{
    private readonly DrawingSession _session;

    public CanvasViewModel(DrawingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _session.Invalidated += OnSessionInvalidated;
    }

    /// <summary>Raised with the area of <see cref="DrawingLayer"/> that must be redrawn.</summary>
    public event EventHandler<CanvasInvalidatedEventArgs>? Invalidated;

    public PixelBuffer DrawingLayer => _session.DrawingLayer;

    public int CanvasWidth => DrawingLayer.Width;

    public int CanvasHeight => DrawingLayer.Height;

    public bool IsStrokeActive => _session.IsStrokeActive;

    public void PointerDown(double x, double y) => _session.BeginStroke(new CanvasPoint(x, y));

    public void PointerMove(double x, double y) => _session.ContinueStroke(new CanvasPoint(x, y));

    public void PointerUp(double x, double y) => _session.EndStroke(new CanvasPoint(x, y));

    public void CancelPointer() => _session.CancelStroke();

    public void Dispose() => _session.Invalidated -= OnSessionInvalidated;

    private void OnSessionInvalidated(object? sender, CanvasInvalidatedEventArgs e) => Invalidated?.Invoke(this, e);
}
