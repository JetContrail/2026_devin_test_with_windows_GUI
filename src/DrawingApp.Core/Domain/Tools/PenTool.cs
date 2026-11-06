using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Rasterization;

namespace DrawingApp.Core.Domain.Tools;

/// <summary>Freehand pen: draws connected round-brush segments between successive pointer positions.</summary>
public sealed class PenTool(ToolOptions options) : ITool
{
    private readonly ToolOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    private PixelBuffer? _target;
    private CanvasPoint _last;
    private int _diameter;
    private RgbaColor _color;

    public ToolKind Kind => ToolKind.Pen;

    public PixelRect PointerDown(PixelBuffer target, CanvasPoint point)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Size and color are fixed for the whole stroke even if the options change mid-drag.
        _target = target;
        _diameter = _options.GetSize(ToolKind.Pen);
        _color = _options.Color;
        _last = point;
        return RoundBrushRasterizer.Stamp(target, point, _diameter, _color, BlendMode.Paint);
    }

    public PixelRect PointerMove(CanvasPoint point)
    {
        if (_target is null)
        {
            return PixelRect.Empty;
        }

        var dirty = RoundBrushRasterizer.Line(_target, _last, point, _diameter, _color, BlendMode.Paint);
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
