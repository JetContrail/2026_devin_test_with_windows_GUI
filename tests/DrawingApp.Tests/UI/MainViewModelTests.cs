using System.ComponentModel;
using System.IO;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
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
    private readonly CanvasViewModel _canvas;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _documents = new DocumentService(_document, _decoder);
        _canvas = new CanvasViewModel(new DrawingSession(_options, [new PenTool(_options)], _document));
        _viewModel = new MainViewModel(_options, _canvas, _documents, _dialogs);
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
    public void NotYetImplementedCommands_AreDisabled()
    {
        Assert.False(_viewModel.SaveDrawingOnlyCommand.CanExecute(null));
        Assert.False(_viewModel.SaveCompositeCommand.CanExecute(null));
        Assert.False(_viewModel.UndoCommand.CanExecute(null));
        Assert.False(_viewModel.RedoCommand.CanExecute(null));
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
    public void Constructor_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, _canvas, _documents, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, null!, _documents, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, _canvas, null!, _dialogs));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, _canvas, _documents, null!));
    }

    [Fact]
    public void ImplementsINotifyPropertyChanged() =>
        Assert.IsAssignableFrom<INotifyPropertyChanged>(_viewModel);
}
