using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Tools;
using DrawingApp.UI.Services;

namespace DrawingApp.UI.ViewModels;

/// <summary>
/// View model of the main window: menu commands, tool selection and toolbar state.
/// Must not depend on System.Windows types so that it stays unit-testable.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ToolOptions _toolOptions;
    private readonly DocumentService _documents;
    private readonly ExportService _export;
    private readonly IDialogService _dialogs;

    public MainViewModel(ToolOptions toolOptions, CanvasViewModel canvas, DocumentService documents, ExportService export, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(toolOptions);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(export);
        ArgumentNullException.ThrowIfNull(dialogs);

        _toolOptions = toolOptions;
        _documents = documents;
        _export = export;
        _dialogs = dialogs;
        Canvas = canvas;
        _toolOptions.Changed += OnToolOptionsChanged;
        Canvas.HistoryChanged += OnHistoryChanged;
        Canvas.ViewportChanged += OnViewportChanged;
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

    /// <summary>Zoom factor (1.0 = 100%).</summary>
    public double ZoomFactor => Canvas.Scale;

    public string ZoomText => $"{ZoomFactor * 100:0}%";

    public void Dispose()
    {
        _toolOptions.Changed -= OnToolOptionsChanged;
        Canvas.HistoryChanged -= OnHistoryChanged;
        Canvas.ViewportChanged -= OnViewportChanged;
    }

    [RelayCommand]
    private void SelectTool(ToolKind tool) => _toolOptions.ActiveTool = tool;

    [RelayCommand]
    private void LoadBackground()
    {
        var path = _dialogs.ShowOpenImageDialog();
        if (path is null)
        {
            return;
        }

        try
        {
            _documents.LoadBackground(path);
        }
        catch (ImageDecodeException ex)
        {
            _dialogs.ShowError($"下絵を読み込めませんでした。\n{ex.Message}");
        }
    }

    [RelayCommand]
    private void SaveDrawingOnly() => Save("描画のみ保存", "drawing.png", _export.SaveDrawingOnly);

    [RelayCommand]
    private void SaveComposite() => Save("合成保存", "composite.png", _export.SaveComposite);

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo() => Canvas.Undo();

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo() => Canvas.Redo();

    private bool CanUndo() => Canvas.CanUndo;

    private bool CanRedo() => Canvas.CanRedo;

    private void Save(string title, string defaultFileName, Action<string> save)
    {
        var path = _dialogs.ShowSaveImageDialog(title, defaultFileName, _export.Encoders);
        if (path is null)
        {
            return;
        }

        try
        {
            save(path);
        }
        catch (ImageSaveException ex)
        {
            _dialogs.ShowError($"保存できませんでした。\n{ex.Message}");
        }
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ZoomFactor));
        OnPropertyChanged(nameof(ZoomText));
    }

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
