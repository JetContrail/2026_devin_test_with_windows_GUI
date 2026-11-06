using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Layers;

namespace DrawingApp.Core.Domain.History;

/// <summary>
/// Raster edit on one layer, stored as the pixels it replaced. Undo and redo swap the stored patch with the
/// layer's current pixels, so only one patch is held at a time.
/// </summary>
public sealed class PixelPatchOperation : IUndoableOperation
{
    private readonly RasterLayer _layer;
    private PixelPatch _stored;
    private bool _isUndone;

    /// <param name="layer">Layer that was edited.</param>
    /// <param name="before">Pixels of the edited area captured before the edit.</param>
    public PixelPatchOperation(RasterLayer layer, PixelPatch before)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(before);

        _layer = layer;
        _stored = before;
    }

    public long SizeInBytes => _stored.SizeInBytes;

    public PixelRect Undo()
    {
        if (_isUndone)
        {
            throw new InvalidOperationException("The operation is already undone.");
        }

        _isUndone = true;
        return Swap();
    }

    public PixelRect Redo()
    {
        if (!_isUndone)
        {
            throw new InvalidOperationException("The operation has not been undone.");
        }

        _isUndone = false;
        return Swap();
    }

    private PixelRect Swap()
    {
        var current = PixelPatch.Capture(_layer.Pixels, _stored.Area);
        _stored.ApplyTo(_layer.Pixels);
        _stored = current;
        return current.Area;
    }
}
