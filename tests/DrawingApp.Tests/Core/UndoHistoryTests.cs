using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public class UndoHistoryTests
{
    private readonly List<string> _log = [];
    private readonly UndoHistory _history = new();

    [Fact]
    public void New_IsEmptyWithDefaults()
    {
        Assert.False(_history.CanUndo);
        Assert.False(_history.CanRedo);
        Assert.Equal(UndoHistory.DefaultCapacity, _history.Capacity);
        Assert.Equal(UndoHistory.DefaultMaxBytes, _history.MaxBytes);
        Assert.True(_history.Undo().IsEmpty);
        Assert.True(_history.Redo().IsEmpty);
    }

    [Fact]
    public void UndoRedo_RunInReverseOrderAndReturnArea()
    {
        _history.Push(Op(1));
        _history.Push(Op(2));

        Assert.Equal(new PixelRect(2, 0, 1, 1), _history.Undo());
        Assert.Equal(new PixelRect(1, 0, 1, 1), _history.Undo());
        Assert.False(_history.CanUndo);
        Assert.Equal(new PixelRect(1, 0, 1, 1), _history.Redo());

        Assert.Equal(["undo 2", "undo 1", "redo 1"], _log);
        Assert.Equal((1, 1), (_history.UndoCount, _history.RedoCount));
    }

    [Fact]
    public void Push_DiscardsRedo()
    {
        _history.Push(Op(1));
        _history.Undo();

        _history.Push(Op(2));

        Assert.False(_history.CanRedo);
        Assert.Equal(1, _history.UndoCount);
    }

    [Fact]
    public void Push_BeyondCapacity_DropsOldest()
    {
        var history = new UndoHistory(UndoHistory.GuaranteedOperations, long.MaxValue);
        for (var i = 1; i <= UndoHistory.GuaranteedOperations + 5; i++)
        {
            history.Push(Op(i));
        }

        Assert.Equal(UndoHistory.GuaranteedOperations, history.UndoCount);
        while (history.CanUndo)
        {
            history.Undo();
        }

        Assert.Equal("undo 6", _log[^1]);
    }

    [Fact]
    public void Push_OverByteLimit_DropsOldestButKeepsGuaranteedCount()
    {
        var history = new UndoHistory(UndoHistory.DefaultCapacity, maxBytes: 100);
        for (var i = 1; i <= 30; i++)
        {
            history.Push(Op(i, size: 50));
        }

        Assert.Equal(UndoHistory.GuaranteedOperations, history.UndoCount);
        Assert.Equal(UndoHistory.GuaranteedOperations * 50, history.SizeInBytes);
    }

    [Fact]
    public void Push_UnderByteLimit_KeepsUpToCapacity()
    {
        for (var i = 1; i <= 60; i++)
        {
            _history.Push(Op(i));
        }

        Assert.Equal(UndoHistory.DefaultCapacity, _history.UndoCount);
    }

    [Fact]
    public void SizeInBytes_IncludesRedoStack()
    {
        _history.Push(Op(1, size: 7));
        _history.Push(Op(2, size: 5));
        _history.Undo();

        Assert.Equal(12, _history.SizeInBytes);
    }

    [Fact]
    public void Clear_RemovesEverything()
    {
        _history.Push(Op(1));
        _history.Push(Op(2));
        _history.Undo();

        _history.Clear();

        Assert.False(_history.CanUndo);
        Assert.False(_history.CanRedo);
    }

    [Fact]
    public void Changed_RaisedOnEveryStateChange()
    {
        var count = 0;
        _history.Changed += (_, _) => count++;

        _history.Push(Op(1));
        _history.Undo();
        _history.Redo();
        _history.Clear();
        _history.Undo();

        Assert.Equal(4, count);
    }

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UndoHistory(UndoHistory.GuaranteedOperations - 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UndoHistory(UndoHistory.DefaultCapacity, -1));
        Assert.Throws<ArgumentNullException>(() => _history.Push(null!));
    }

    private RecordingOperation Op(int id, long size = 1) => new(id, size, _log);

    private sealed class RecordingOperation(int id, long size, List<string> log) : IUndoableOperation
    {
        public long SizeInBytes => size;

        public PixelRect Undo()
        {
            log.Add($"undo {id}");
            return new PixelRect(id, 0, 1, 1);
        }

        public PixelRect Redo()
        {
            log.Add($"redo {id}");
            return new PixelRect(id, 0, 1, 1);
        }
    }
}
