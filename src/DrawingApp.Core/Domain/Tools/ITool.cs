using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.Tools;

/// <summary>
/// A pointer-driven editing tool. A stroke is <see cref="PointerDown"/>, any number of <see cref="PointerMove"/> and <see cref="PointerUp"/>;
/// each call returns the area of <c>target</c> it changed.
/// </summary>
public interface ITool
{
    ToolKind Kind { get; }

    PixelRect PointerDown(PixelBuffer target, CanvasPoint point);

    PixelRect PointerMove(CanvasPoint point);

    PixelRect PointerUp(CanvasPoint point);
}
