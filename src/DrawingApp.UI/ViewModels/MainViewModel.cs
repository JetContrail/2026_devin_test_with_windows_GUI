using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.UI.ViewModels;

/// <summary>
/// View model of the main window: menu commands, tool selection and toolbar state.
/// Must not depend on System.Windows types so that it stays unit-testable.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ToolOptions _toolOptions;

    public MainViewModel(ToolOptions toolOptions, CanvasViewModel canvas)
    {
        ArgumentNullException.ThrowIfNull(toolOptions);
        ArgumentNullException.ThrowIfNull(canvas);

        _toolOptions = toolOptions;
        Canvas = canvas;
        _toolOptions.Changed += OnToolOptionsChanged;
    }

    public string Title => "DrawingApp";

    public CanvasViewModel Canvas { get; }

    public IReadOnlyList<int> PenSizes => BrushSizePresets.Pen;

    public IReadOnlyList<int> DotSizes => BrushSizePresets.Dot;

    public IReadOnlyList<int> EraserSizes => BrushSizePresets.Eraser;

    public ToolKind ActiveTool => _toolOptions.ActiveTool;

    public bool IsPenActive => ActiveTool == ToolKind.Pen;

    public bool IsDotActive => ActiveTool == ToolKind.Dot;

    public bool IsEraserActive => ActiveTool == ToolKind.Eraser;

    public int PenSize
    {
        get => _toolOptions.GetSize(ToolKind.Pen);
        set => _toolOptions.SetSize(ToolKind.Pen, value);
    }

    public int DotSize
    {
        get => _toolOptions.GetSize(ToolKind.Dot);
        set => _toolOptions.SetSize(ToolKind.Dot, value);
    }

    public int EraserSize
    {
        get => _toolOptions.GetSize(ToolKind.Eraser);
        set => _toolOptions.SetSize(ToolKind.Eraser, value);
    }

    /// <summary>Zoom factor (1.0 = 100%). Becomes interactive in Phase 7.</summary>
    public double ZoomFactor => 1.0;

    public string ZoomText => $"{ZoomFactor * 100:0}%";

    public void Dispose() => _toolOptions.Changed -= OnToolOptionsChanged;

    [RelayCommand]
    private void SelectTool(ToolKind tool) => _toolOptions.ActiveTool = tool;

    // Commands below are wired to the menu now and implemented in later phases (see docs/Roadmap.md).
    [RelayCommand(CanExecute = nameof(IsFeatureAvailable))]
    private void LoadBackground()
    {
    }

    [RelayCommand(CanExecute = nameof(IsFeatureAvailable))]
    private void SaveDrawingOnly()
    {
    }

    [RelayCommand(CanExecute = nameof(IsFeatureAvailable))]
    private void SaveComposite()
    {
    }

    [RelayCommand(CanExecute = nameof(IsFeatureAvailable))]
    private void Undo()
    {
    }

    [RelayCommand(CanExecute = nameof(IsFeatureAvailable))]
    private void Redo()
    {
    }

    private static bool IsFeatureAvailable() => false;

    private void OnToolOptionsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ActiveTool));
        OnPropertyChanged(nameof(IsPenActive));
        OnPropertyChanged(nameof(IsDotActive));
        OnPropertyChanged(nameof(IsEraserActive));
        OnPropertyChanged(nameof(PenSize));
        OnPropertyChanged(nameof(DotSize));
        OnPropertyChanged(nameof(EraserSize));
    }
}
