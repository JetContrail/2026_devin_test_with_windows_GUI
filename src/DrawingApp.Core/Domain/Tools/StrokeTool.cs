using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Rasterization;

namespace DrawingApp.Core.Domain.Tools;

/// <summary>Drag tool that applies connected round-brush segments between successive pointer positions.</summary>
public abstract class StrokeTool : ITool
{
    private readonly ToolOptions _options;
    private readonly BlendMode _mode;

    private PixelBuffer? _target;
    private CanvasPoint _last;
    private int _diameter;
    private RgbaColor _color;

    protected StrokeTool(ToolOptions options, ToolKind kind, BlendMode mode)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        Kind = kind;
        _mode = mode;
    }

    public ToolKind Kind { get; }

    public PixelRect PointerDown(PixelBuffer target, CanvasPoint point)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Size and color are fixed for the whole stroke even if the options change mid-drag.
        _target = target;
        _diameter = _options.GetSize(Kind);
        _color = _options.Color;
        _last = point;
        return RoundBrushRasterizer.Stamp(target, point, _diameter, _color, _mode);
    }

    public PixelRect PointerMove(CanvasPoint point)
    {
        if (_target is null)
        {
            return PixelRect.Empty;
        }

        var dirty = RoundBrushRasterizer.Line(_target, _last, point, _diameter, _color, _mode);
        _last = point;
        return dirty;
    }

    public PixelRect PointerUp(CanvasPoint point)
    {
        var dirty = PointerMove(point);
        _target = null;
        return dirty;
    }
}
