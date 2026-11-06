namespace DrawingApp.Core.Domain.Tools;

/// <summary>
/// Current tool selection and per-tool brush size. Shared between the UI and the drawing session.
/// </summary>
public sealed class ToolOptions
{
    private readonly Dictionary<ToolKind, int> _sizes = Enum.GetValues<ToolKind>()
        .ToDictionary(tool => tool, BrushSizePresets.DefaultFor);

    private ToolKind _activeTool = ToolKind.Pen;

    /// <summary>Raised after <see cref="ActiveTool"/> or a brush size changes.</summary>
    public event EventHandler? Changed;

    public ToolKind ActiveTool
    {
        get => _activeTool;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown tool.");
            }

            if (_activeTool != value)
            {
                _activeTool = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Brush size of the active tool.</summary>
    public int ActiveSize => GetSize(_activeTool);

    public int GetSize(ToolKind tool) => _sizes.TryGetValue(tool, out var size)
        ? size
        : throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool.");

    /// <summary>Sets the brush size of <paramref name="tool"/>; the size must be one of its presets.</summary>
    public void SetSize(ToolKind tool, int size)
    {
        if (!BrushSizePresets.For(tool).Contains(size))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"{size}px is not a preset size for {tool}.");
        }

        if (_sizes[tool] != size)
        {
            _sizes[tool] = size;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
