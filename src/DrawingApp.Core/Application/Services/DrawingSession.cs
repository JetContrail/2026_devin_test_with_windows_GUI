using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Core.Application.Services;

/// <summary>
/// Routes pointer strokes to the active <see cref="ITool"/>, which only ever edits the document's active (drawing) layer.
/// Raises <see cref="Invalidated"/> with the changed area so views can redraw only that region.
/// </summary>
public sealed class DrawingSession
{
    private readonly ToolOptions _options;
    private readonly Dictionary<ToolKind, ITool> _tools;

    private ITool? _activeTool;
    private CanvasPoint _lastPoint;

    public DrawingSession(ToolOptions options, IEnumerable<ITool> tools, DrawingDocument document)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(document);

        _options = options;
        _tools = tools.ToDictionary(tool => tool.Kind);
        Document = document;
        // Buffers are replaced on reset, so an in-flight stroke is dropped rather than finished on the old buffer.
        Document.Reset += (_, _) => _activeTool = null;
    }

    public event EventHandler<CanvasInvalidatedEventArgs>? Invalidated;

    public DrawingDocument Document { get; }

    /// <summary>Pixels of the layer edited by the tools. The background layer is never edited.</summary>
    public PixelBuffer DrawingLayer => Document.ActiveLayer.Pixels;

    public bool IsStrokeActive => _activeTool is not null;

    public void BeginStroke(CanvasPoint point)
    {
        if (_activeTool is not null || !_tools.TryGetValue(_options.ActiveTool, out var tool))
        {
            return;
        }

        _activeTool = tool;
        _lastPoint = point;
        Raise(tool.PointerDown(DrawingLayer, point));
    }

    public void ContinueStroke(CanvasPoint point)
    {
        if (_activeTool is null)
        {
            return;
        }

        _lastPoint = point;
        Raise(_activeTool.PointerMove(point));
    }

    public void EndStroke(CanvasPoint point)
    {
        if (_activeTool is null)
        {
            return;
        }

        var tool = _activeTool;
        _activeTool = null;
        _lastPoint = point;
        Raise(tool.PointerUp(point));
    }

    /// <summary>Ends the stroke at the last known position (e.g. when mouse capture is lost).</summary>
    public void CancelStroke() => EndStroke(_lastPoint);

    private void Raise(PixelRect area)
    {
        if (!area.IsEmpty)
        {
            Invalidated?.Invoke(this, new CanvasInvalidatedEventArgs(area));
        }
    }
}
