namespace DrawingApp.Core.Domain.Tools;

/// <summary>Selectable brush diameters (px) per tool.</summary>
public static class BrushSizePresets
{
    public static IReadOnlyList<int> Pen { get; } = [1, 3, 5, 10, 20];

    public static IReadOnlyList<int> Dot { get; } = [1, 3, 5, 10, 20];

    public static IReadOnlyList<int> Eraser { get; } = [10, 20, 50];

    public static IReadOnlyList<int> For(ToolKind tool) => tool switch
    {
        ToolKind.Pen => Pen,
        ToolKind.Dot => Dot,
        ToolKind.Eraser => Eraser,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool."),
    };

    public static int DefaultFor(ToolKind tool) => tool switch
    {
        ToolKind.Pen => 3,
        ToolKind.Dot => 5,
        ToolKind.Eraser => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown tool."),
    };
}
