using System.ComponentModel;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.Tests.UI;

public sealed class MainViewModelTests : IDisposable
{
    private readonly ToolOptions _options = new();
    private readonly CanvasViewModel _canvas;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _canvas = new CanvasViewModel(new DrawingSession(_options, [new PenTool(_options)]));
        _viewModel = new MainViewModel(_options, _canvas);
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
        Assert.False(_viewModel.LoadBackgroundCommand.CanExecute(null));
        Assert.False(_viewModel.SaveDrawingOnlyCommand.CanExecute(null));
        Assert.False(_viewModel.SaveCompositeCommand.CanExecute(null));
        Assert.False(_viewModel.UndoCommand.CanExecute(null));
        Assert.False(_viewModel.RedoCommand.CanExecute(null));
    }

    [Fact]
    public void Constructor_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!, _canvas));
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(_options, null!));
    }

    [Fact]
    public void ImplementsINotifyPropertyChanged() =>
        Assert.IsAssignableFrom<INotifyPropertyChanged>(_viewModel);
}
