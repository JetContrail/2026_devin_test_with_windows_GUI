using CommunityToolkit.Mvvm.ComponentModel;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Viewport;

namespace DrawingApp.UI.ViewModels;

/// <summary>
/// Exposes the layers to the canvas view and forwards pointer input (in canvas pixels) to the <see cref="DrawingSession"/>.
/// </summary>
public sealed class CanvasViewModel : ObservableObject, IDisposable
{
    /// <summary>WPF reports one standard wheel notch as a delta of 120.</summary>
    public const int WheelDeltaPerNotch = 120;

    private readonly DrawingSession _session;
    private readonly ViewportState _viewport;
    private int _pendingWheelDelta;
    private double _panX;
    private double _panY;

    public CanvasViewModel(DrawingSession session, ViewportState viewport)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(viewport);

        _session = session;
        _viewport = viewport;
        _viewport.Changed += OnViewportChanged;
        _session.Invalidated += OnSessionInvalidated;
        _session.Document.Reset += OnDocumentReset;
        _session.History.Changed += OnHistoryChanged;
    }

    /// <summary>Raised with the area of <see cref="DrawingLayer"/> that must be redrawn.</summary>
    public event EventHandler<CanvasInvalidatedEventArgs>? Invalidated;

    /// <summary>Raised when the layer buffers were replaced and the view must recreate its bitmaps.</summary>
    public event EventHandler? LayersReset;

    /// <summary>Raised when <see cref="CanUndo"/> or <see cref="CanRedo"/> may have changed.</summary>
    public event EventHandler? HistoryChanged;

    /// <summary>Raised after <see cref="Scale"/> or the offset changed.</summary>
    public event EventHandler? ViewportChanged;

    public PixelBuffer BackgroundLayer => _session.Document.Background.Pixels;

    public PixelBuffer DrawingLayer => _session.DrawingLayer;

    public int CanvasWidth => DrawingLayer.Width;

    public int CanvasHeight => DrawingLayer.Height;

    public bool IsStrokeActive => _session.IsStrokeActive;

    public bool CanUndo => _session.History.CanUndo;

    public bool CanRedo => _session.History.CanRedo;

    public double Scale => _viewport.Scale;

    public double OffsetX => _viewport.OffsetX;

    public double OffsetY => _viewport.OffsetY;

    public bool IsPanning { get; private set; }

    public void PointerDown(double x, double y) => _session.BeginStroke(new CanvasPoint(x, y));

    public void PointerMove(double x, double y) => _session.ContinueStroke(new CanvasPoint(x, y));

    public void PointerUp(double x, double y) => _session.EndStroke(new CanvasPoint(x, y));

    public void CancelPointer() => _session.CancelStroke();

    public void Undo() => _session.Undo();

    public void Redo() => _session.Redo();

    /// <summary>
    /// Zooms around a viewport position. Deltas smaller than one notch (high-resolution wheels, touchpads)
    /// accumulate until they add up to a notch.
    /// </summary>
    public void Zoom(double viewX, double viewY, int wheelDelta)
    {
        _pendingWheelDelta += wheelDelta;
        var notches = _pendingWheelDelta / WheelDeltaPerNotch;
        if (notches != 0)
        {
            _pendingWheelDelta -= notches * WheelDeltaPerNotch;
            _viewport.ZoomAt(viewX, viewY, notches);
        }
    }

    /// <summary>Starts panning from a viewport position. Refused while a stroke is being drawn.</summary>
    public bool BeginPan(double viewX, double viewY)
    {
        if (IsPanning || IsStrokeActive)
        {
            return false;
        }

        IsPanning = true;
        (_panX, _panY) = (viewX, viewY);
        return true;
    }

    public void PanTo(double viewX, double viewY)
    {
        if (!IsPanning)
        {
            return;
        }

        _viewport.PanBy(viewX - _panX, viewY - _panY);
        (_panX, _panY) = (viewX, viewY);
    }

    public void EndPan() => IsPanning = false;

    public void Dispose()
    {
        _session.Invalidated -= OnSessionInvalidated;
        _session.Document.Reset -= OnDocumentReset;
        _session.History.Changed -= OnHistoryChanged;
        _viewport.Changed -= OnViewportChanged;
    }

    private void OnSessionInvalidated(object? sender, CanvasInvalidatedEventArgs e) => Invalidated?.Invoke(this, e);

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Scale));
        OnPropertyChanged(nameof(OffsetX));
        OnPropertyChanged(nameof(OffsetY));
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnHistoryChanged(object? sender, EventArgs e) => HistoryChanged?.Invoke(this, e);

    private void OnDocumentReset(object? sender, EventArgs e)
    {
        LayersReset?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
        IsPanning = false;
        _viewport.Reset();
    }
}
