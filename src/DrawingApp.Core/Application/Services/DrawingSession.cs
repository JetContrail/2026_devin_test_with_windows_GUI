using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Core.Application.Services;

/// <summary>
/// Routes pointer strokes to the active <see cref="ITool"/>, which only ever edits the document's active (drawing) layer.
/// Raises <see cref="Invalidated"/> with the changed area so views can redraw only that region.
/// Each finished stroke is recorded in <see cref="History"/> as one <see cref="PixelPatchOperation"/>.
/// </summary>
public sealed class DrawingSession
{
    private readonly ToolOptions _options;
    private readonly Dictionary<ToolKind, ITool> _tools;

    private ITool? _activeTool;
    private CanvasPoint _lastPoint;
    private PixelRect _strokeArea;

    // Drawing layer as of the last recorded operation; the "before" pixels of the current stroke are read from here.
    private PixelBuffer? _committed;

    public DrawingSession(ToolOptions options, IEnumerable<ITool> tools, DrawingDocument document, UndoHistory history)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);

        _options = options;
        _tools = tools.ToDictionary(tool => tool.Kind);
        Document = document;
        History = history;
        Document.Reset += OnDocumentReset;
    }

    public event EventHandler<CanvasInvalidatedEventArgs>? Invalidated;

    public DrawingDocument Document { get; }

    public UndoHistory History { get; }

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
        _strokeArea = PixelRect.Empty;
        _committed ??= DrawingLayer.Resized(DrawingLayer.Width, DrawingLayer.Height);
        Apply(tool.PointerDown(DrawingLayer, point));
    }

    public void ContinueStroke(CanvasPoint point)
    {
        if (_activeTool is null)
        {
            return;
        }

        _lastPoint = point;
        Apply(_activeTool.PointerMove(point));
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
        Apply(tool.PointerUp(point));
        Commit();
    }

    /// <summary>Ends the stroke at the last known position (e.g. when mouse capture is lost).</summary>
    public void CancelStroke() => EndStroke(_lastPoint);

    /// <summary>Reverts the latest stroke. Ignored while a stroke is in progress.</summary>
    public void Undo()
    {
        if (_activeTool is null)
        {
            Sync(History.Undo());
        }
    }

    /// <summary>Re-applies the latest undone stroke. Ignored while a stroke is in progress.</summary>
    public void Redo()
    {
        if (_activeTool is null)
        {
            Sync(History.Redo());
        }
    }

    private void Apply(PixelRect area)
    {
        _strokeArea = _strokeArea.Union(area);
        Raise(area);
    }

    private void Commit()
    {
        if (_strokeArea.IsEmpty || _committed is null)
        {
            return;
        }

        History.Push(new PixelPatchOperation(Document.ActiveLayer, PixelPatch.Capture(_committed, _strokeArea)));
        _committed.CopyFrom(DrawingLayer, _strokeArea);
    }

    private void Sync(PixelRect area)
    {
        if (!area.IsEmpty)
        {
            _committed?.CopyFrom(DrawingLayer, area);
            Raise(area);
        }
    }

    // Buffers are replaced on reset, so an in-flight stroke is dropped and the history no longer applies (A4).
    private void OnDocumentReset(object? sender, EventArgs e)
    {
        _activeTool = null;
        _committed = null;
        History.Clear();
    }

    private void Raise(PixelRect area)
    {
        if (!area.IsEmpty)
        {
            Invalidated?.Invoke(this, new CanvasInvalidatedEventArgs(area));
        }
    }
}
