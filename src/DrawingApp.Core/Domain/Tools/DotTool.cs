using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Rasterization;

namespace DrawingApp.Core.Domain.Tools;

/// <summary>Places a single round dot at the pressed position; dragging does not add more dots.</summary>
public sealed class DotTool(ToolOptions options) : ITool
{
    private readonly ToolOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public ToolKind Kind => ToolKind.Dot;

    public PixelRect PointerDown(PixelBuffer target, CanvasPoint point)
    {
        ArgumentNullException.ThrowIfNull(target);

        return RoundBrushRasterizer.Stamp(target, point, _options.GetSize(ToolKind.Dot), _options.Color, BlendMode.Paint);
    }

    public PixelRect PointerMove(CanvasPoint point) => PixelRect.Empty;

    public PixelRect PointerUp(CanvasPoint point) => PixelRect.Empty;
}
