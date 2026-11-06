using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.History;

/// <summary>One user operation that <see cref="UndoHistory"/> can revert and re-apply.</summary>
public interface IUndoableOperation
{
    /// <summary>Memory held by the operation, used for the history's byte limit.</summary>
    long SizeInBytes { get; }

    /// <summary>Reverts the operation and returns the changed canvas area.</summary>
    PixelRect Undo();

    /// <summary>Re-applies a reverted operation and returns the changed canvas area.</summary>
    PixelRect Redo();
}
