using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Application.Services;

public sealed class CanvasInvalidatedEventArgs(PixelRect area) : EventArgs
{
    public PixelRect Area { get; } = area;
}
