using DrawingApp.Core.Domain.Rasterization;

namespace DrawingApp.Core.Domain.Tools;

/// <summary>
/// Drag eraser: makes covered pixels of the edited layer transparent. Tools only receive the drawing layer,
/// so the background can never be erased.
/// </summary>
public sealed class EraserTool(ToolOptions options) : StrokeTool(options, ToolKind.Eraser, BlendMode.Erase);
