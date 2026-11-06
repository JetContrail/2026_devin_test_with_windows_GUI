using DrawingApp.Core.Domain.Rasterization;

namespace DrawingApp.Core.Domain.Tools;

/// <summary>Freehand pen drawing with the current color.</summary>
public sealed class PenTool(ToolOptions options) : StrokeTool(options, ToolKind.Pen, BlendMode.Paint);
