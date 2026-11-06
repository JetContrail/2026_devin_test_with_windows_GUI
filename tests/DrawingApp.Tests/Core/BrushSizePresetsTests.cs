using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Tests.Core;

public class BrushSizePresetsTests
{
    [Fact]
    public void Pen_ContainsRequiredSizes() =>
        Assert.Equal([1, 3, 5, 10, 20], BrushSizePresets.Pen);

    [Fact]
    public void Dot_ContainsRequiredSizes() =>
        Assert.Equal([1, 3, 5, 10, 20], BrushSizePresets.Dot);

    [Fact]
    public void Eraser_ContainsRequiredSizes() =>
        Assert.Equal([10, 20, 50], BrushSizePresets.Eraser);

    [Theory]
    [InlineData(ToolKind.Pen)]
    [InlineData(ToolKind.Dot)]
    [InlineData(ToolKind.Eraser)]
    public void DefaultFor_IsOneOfThePresets(ToolKind tool) =>
        Assert.Contains(BrushSizePresets.DefaultFor(tool), BrushSizePresets.For(tool));

    [Fact]
    public void For_UnknownTool_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BrushSizePresets.For((ToolKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => BrushSizePresets.DefaultFor((ToolKind)99));
    }
}
