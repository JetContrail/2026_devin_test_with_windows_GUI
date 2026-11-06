using CommunityToolkit.Mvvm.ComponentModel;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.UI.ViewModels;

/// <summary>
/// Exposes the layers to the canvas view and forwards pointer input (in canvas pixels) to the <see cref="DrawingSession"/>.
/// </summary>
public sealed class CanvasViewModel : ObservableObject, IDisposable
{
    private readonly DrawingSession _session;

    public CanvasViewModel(DrawingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
        _session.Invalidated += OnSessionInvalidated;
        _session.Document.Reset += OnDocumentReset;
    }

    /// <summary>Raised with the area of <see cref="DrawingLayer"/> that must be redrawn.</summary>
    public event EventHandler<CanvasInvalidatedEventArgs>? Invalidated;

    /// <summary>Raised when the layer buffers were replaced and the view must recreate its bitmaps.</summary>
    public event EventHandler? LayersReset;

    public PixelBuffer BackgroundLayer => _session.Document.Background.Pixels;

    public PixelBuffer DrawingLayer => _session.DrawingLayer;

    public int CanvasWidth => DrawingLayer.Width;

    public int CanvasHeight => DrawingLayer.Height;

    public bool IsStrokeActive => _session.IsStrokeActive;

    public void PointerDown(double x, double y) => _session.BeginStroke(new CanvasPoint(x, y));

    public void PointerMove(double x, double y) => _session.ContinueStroke(new CanvasPoint(x, y));

    public void PointerUp(double x, double y) => _session.EndStroke(new CanvasPoint(x, y));

    public void CancelPointer() => _session.CancelStroke();

    public void Dispose()
    {
        _session.Invalidated -= OnSessionInvalidated;
        _session.Document.Reset -= OnDocumentReset;
    }

    private void OnSessionInvalidated(object? sender, CanvasInvalidatedEventArgs e) => Invalidated?.Invoke(this, e);

    private void OnDocumentReset(object? sender, EventArgs e)
    {
        LayersReset?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
    }
}
