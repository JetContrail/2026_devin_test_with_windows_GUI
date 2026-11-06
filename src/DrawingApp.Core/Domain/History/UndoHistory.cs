using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Domain.History;

/// <summary>
/// Undo/redo stacks. The oldest operations are dropped beyond <see cref="Capacity"/>, or beyond
/// <see cref="MaxBytes"/> as long as at least <see cref="GuaranteedOperations"/> remain undoable.
/// </summary>
public sealed class UndoHistory
{
    public const int GuaranteedOperations = 20;
    public const int DefaultCapacity = 50;
    public const long DefaultMaxBytes = 64L * 1024 * 1024;

    private readonly LinkedList<IUndoableOperation> _undo = new();
    private readonly Stack<IUndoableOperation> _redo = new();

    public UndoHistory()
        : this(DefaultCapacity, DefaultMaxBytes)
    {
    }

    public UndoHistory(int capacity, long maxBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, GuaranteedOperations);
        ArgumentOutOfRangeException.ThrowIfNegative(maxBytes);

        Capacity = capacity;
        MaxBytes = maxBytes;
    }

    /// <summary>Raised whenever <see cref="CanUndo"/> or <see cref="CanRedo"/> may have changed.</summary>
    public event EventHandler? Changed;

    public int Capacity { get; }

    public long MaxBytes { get; }

    public int UndoCount => _undo.Count;

    public int RedoCount => _redo.Count;

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public long SizeInBytes => _undo.Sum(op => op.SizeInBytes) + _redo.Sum(op => op.SizeInBytes);

    /// <summary>Records an operation that has already been applied. Discards everything that could be redone.</summary>
    public void Push(IUndoableOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        _redo.Clear();
        _undo.AddLast(operation);
        while (_undo.Count > Capacity || (_undo.Count > GuaranteedOperations && SizeInBytes > MaxBytes))
        {
            _undo.RemoveFirst();
        }

        OnChanged();
    }

    /// <summary>Reverts the latest operation. Returns the changed area, or empty when there is nothing to undo.</summary>
    public PixelRect Undo()
    {
        if (_undo.Last is not { } node)
        {
            return PixelRect.Empty;
        }

        _undo.RemoveLast();
        var area = node.Value.Undo();
        _redo.Push(node.Value);
        OnChanged();
        return area;
    }

    /// <summary>Re-applies the latest undone operation. Returns the changed area, or empty when there is nothing to redo.</summary>
    public PixelRect Redo()
    {
        if (!_redo.TryPop(out var operation))
        {
            return PixelRect.Empty;
        }

        var area = operation.Redo();
        _undo.AddLast(operation);
        OnChanged();
        return area;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        OnChanged();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
