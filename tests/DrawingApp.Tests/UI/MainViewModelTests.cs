using System.ComponentModel;
using System.IO;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.Tests.Core;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.Tests.UI;

public sealed class MainViewModelTests : IDisposable
{
    private readonly ToolOptions _options = new();
    private readonly DrawingDocument _document = new();
    private readonly FakeImageDecoder _decoder = new(40, 30);
    private readonly FakeDialogService _dialogs = new();
    private readonly DocumentService _documents;
    private readonly ExportService _export;
    private readonly CanvasViewModel _canvas;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _documents = new DocumentService(_document, _decoder);
        _export = new ExportService(_document, [new FakeImageEncoder()]);
        _canvas = new CanvasViewModel(new DrawingSession(_options, [new PenTool(_options)], _document, new UndoHistory()));
        _viewModel = new MainViewModel(_options, _canvas, _documents, _export, _dialogs);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _canvas.Dispose();
    }

    [Fact]
    public void Initial_State_ShowsPenAnd100Percent()
    {
        Assert.Equal("DrawingApp", _viewModel.Title);
        Assert.Equal(ToolKind.Pen, _viewModel.ActiveTool);
        Assert.True(_viewModel.IsPenActive);
        Assert.False(_viewModel.IsDotActive);
        Assert.False(_viewModel.IsEraserActive);
        Assert.Equal("100%", _viewModel.ZoomText);
        Assert.Same(_canvas, _viewModel.Canvas);
    }

    [Fact]
    public void SizeLists_ExposePresets()
    {
        Assert.Equal(BrushSizePresets.Pen, _viewModel.PenSizes);
        Assert.Equal(BrushSizePresets.Dot, _viewModel.DotSizes);
        Assert.Equal(BrushSizePresets.Eraser, _viewModel.EraserSizes);
    }

    [Theory]
    [InlineData(ToolKind.Dot)]
    [InlineData(ToolKind.Eraser)]
    public void SelectToolCommand_ChangesActiveToolAndRaisesFlags(ToolKind tool)
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _viewModel.SelectToolCommand.Execute(tool);

        Assert.Equal(tool, _options.ActiveTool);
        Assert.False(_viewModel.IsPenActive);
        Assert.Contains(nameof(MainViewModel.IsPenActive), changed);
        Assert.Contains(nameof(MainViewModel.ActiveTool), changed);
    }

    [Fact]
    public void SizeProperties_WriteThroughToToolOptions()
    {
        _viewModel.PenSize = 20;
        _viewModel.DotSize = 10;
        _viewModel.EraserSize = 50;

        Assert.Equal(20, _options.GetSize(ToolKind.Pen));
        Assert.Equal(10, _options.GetSize(ToolKind.Dot));
        Assert.Equal(50, _options.GetSize(ToolKind.Eraser));
    }

    [Fact]
    public void ToolOptionsChange_RaisesSizePropertyChanged()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _options.SetSize(ToolKind.Eraser, 10);

        Assert.Contains(nameof(MainViewModel.EraserSize), changed);
        Assert.Equal(10, _viewModel.EraserSize);
    }

    [Fact]
    public void Dispose_UnsubscribesFromToolOptions()
    {
        var changed = 0;
        _viewModel.PropertyChanged += (_, _) => changed++;
        _viewModel.Dispose();

        _options.ActiveTool = ToolKind.Dot;

        Assert.Equal(0, changed);
    }

    [Fact]
    public void UndoRedo_InitiallyDisabled()
    {
        Assert.False(_viewModel.UndoCommand.CanExecute(null));
        Assert.False(_viewModel.RedoCommand.CanExecute(null));
    }

    [Fact]
    public void Undo_AfterStroke_RevertsAndEnablesRedo()
    {
        var notified = 0;
        _viewModel.UndoCommand.CanExecuteChanged += (_, _) => notified++;
        Stroke();
        Assert.True(_viewModel.UndoCommand.CanExecute(null));

        _viewModel.UndoCommand.Execute(null);

        Assert.Equal(RgbaColor.Transparent, _canvas.DrawingLayer.GetPixel(10, 10));
        Assert.False(_viewModel.UndoCommand.CanExecute(null));
        Assert.True(_viewModel.RedoCommand.CanExecute(null));
        Assert.Equal(2, notified);
    }

    [Fact]
    public void Redo_ReappliesStroke()
    {
        Stroke();
        _viewModel.UndoCommand.Execute(null);

        _viewModel.RedoCommand.Execute(null);

        Assert.Equal(RgbaColor.Black, _canvas.DrawingLayer.GetPixel(10, 10));
        Assert.True(_viewModel.UndoCommand.CanExecute(null));
        Assert.False(_viewModel.RedoCommand.CanExecute(null));
    }

    [Fact]
    public void LoadBackground_ClearsUndoHistory()
    {
        var path = Path.GetTempFileName();
        try
        {
            Stroke();
            _dialogs.OpenImagePath = path;

            _viewModel.LoadBackgroundCommand.Execute(null);

            Assert.False(_viewModel.UndoCommand.CanExecute(null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_StopsHistoryNotifications()
    {
        var notified = 0;
        _viewModel.UndoCommand.CanExecuteChanged += (_, _) => notified++;
        _viewModel.Dispose();

        Stroke();

        Assert.Equal(0, notified);
    }

    private void Stroke()
    {
        _canvas.PointerDown(10, 10);
        _canvas.PointerUp(20, 10);
    }

    [Fact]
    public void LoadBackground_ChosenFile_ResizesCanvas()
    {
        var path = Path.GetTempFileName();
        try
        {
            _dialogs.OpenImagePath = path;

            Assert.True(_viewModel.LoadBackgroundCommand.CanExecute(null));
            _viewModel.LoadBackgroundCommand.Execute(null);

            Assert.Equal((40, 30), (_canvas.CanvasWidth, _canvas.CanvasHeight));
            Assert.Empty(_dialogs.Errors);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadBackground_Cancelled_DoesNothing()
    {
        _viewModel.LoadBackgroundCommand.Execute(null);

        Assert.Equal(0, _decoder.DecodeCount);
        Assert.Equal(DrawingDocument.DefaultWidth, _canvas.CanvasWidth);
    }

    [Fact]
    public void LoadBackground_InvalidFile_ShowsErrorAndKeepsDocument()
    {
        _dialogs.OpenImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");

        _viewModel.LoadBackgroundCommand.Execute(null);

        Assert.Single(_dialogs.Errors);
        Assert.Contains("下絵を読み込めませんでした", _dialogs.Errors[0], StringComparison.Ordinal);
        Assert.Equal(DrawingDocument.DefaultWidth, _canvas.CanvasWidth);
    }

    [Fact]
    public void SaveDrawingOnly_WritesDrawingLayerToChosenFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.raw");
        _document.ActiveLayer.Pixels.SetPixel(0, 0, RgbaColor.Black);
        _dialogs.SaveImagePath = path;
        try
        {
            _viewModel.SaveDrawingOnlyCommand.Execute(null);

            var request = Assert.Single(_dialogs.SaveRequests);
            Assert.Equal(("描画のみ保存", "drawing.png"), (request.Title, request.DefaultFileName));
            Assert.Same(_export.Encoders, request.Encoders);
            Assert.Equal(_document.ActiveLayer.Pixels.GetBuffer(), FakeImageEncoder.Read(path).GetBuffer());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveComposite_WritesFlattenedImage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.raw");
        _dialogs.SaveImagePath = path;
        try
        {
            _viewModel.SaveCompositeCommand.Execute(null);

            Assert.Equal("composite.png", Assert.Single(_dialogs.SaveRequests).DefaultFileName);
            Assert.Equal(RgbaColor.White, FakeImageEncoder.Read(path).GetPixel(0, 0));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Save_Cancelled_WritesNothing()
    {
        _viewModel.SaveCompositeCommand.Execute(null);

        Assert.Single(_dialogs.SaveRequests);
        Assert.Empty(_dialogs.Errors);
    }

    [Fact]
    public void Save_Failure_ShowsError()
    {
        _dialogs.SaveImagePath = Path.Combine(Path.GetTempPath(), "unsupported.bmp");

        _viewModel.SaveDrawingOnlyCommand.Execute(null);

        Assert.Contains("保存できませんでした", Assert.Single(_dialogs.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, _canvas, _documents, _export, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, null!, _documents, _export, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, _canvas, null!, _export, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, _canvas, _documents, null!, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, _canvas, _documents, _export, null!));
    }

    [Fact]
    public void ImplementsINotifyPropertyChanged() =>
        Assert.IsAssignableFrom<INotifyPropertyChanged>(_viewModel);
}
