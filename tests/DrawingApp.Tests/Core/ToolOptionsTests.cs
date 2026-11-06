using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Tests.Core;

public class ToolOptionsTests
{
    [Fact]
    public void New_DefaultsToPenWithPresetDefaults()
    {
        var options = new ToolOptions();

        Assert.Equal(ToolKind.Pen, options.ActiveTool);
        Assert.Equal(3, options.GetSize(ToolKind.Pen));
        Assert.Equal(5, options.GetSize(ToolKind.Dot));
        Assert.Equal(20, options.GetSize(ToolKind.Eraser));
        Assert.Equal(3, options.ActiveSize);
    }

    [Fact]
    public void ActiveTool_Changed_RaisesEventAndUpdatesActiveSize()
    {
        var options = new ToolOptions();
        var raised = 0;
        options.Changed += (_, _) => raised++;

        options.ActiveTool = ToolKind.Eraser;

        Assert.Equal(1, raised);
        Assert.Equal(20, options.ActiveSize);
    }

    [Fact]
    public void ActiveTool_SameValue_DoesNotRaiseEvent()
    {
        var options = new ToolOptions();
        var raised = 0;
        options.Changed += (_, _) => raised++;

        options.ActiveTool = ToolKind.Pen;

        Assert.Equal(0, raised);
    }

    [Fact]
    public void ActiveTool_Undefined_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToolOptions().ActiveTool = (ToolKind)99);

    [Theory]
    [InlineData(ToolKind.Pen, 20)]
    [InlineData(ToolKind.Dot, 1)]
    [InlineData(ToolKind.Eraser, 50)]
    public void SetSize_PresetValue_IsStoredPerTool(ToolKind tool, int size)
    {
        var options = new ToolOptions();
        var raised = 0;
        options.Changed += (_, _) => raised++;

        options.SetSize(tool, size);

        Assert.Equal(size, options.GetSize(tool));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetSize_DoesNotAffectOtherTools()
    {
        var options = new ToolOptions();

        options.SetSize(ToolKind.Pen, 10);

        Assert.Equal(5, options.GetSize(ToolKind.Dot));
        Assert.Equal(20, options.GetSize(ToolKind.Eraser));
    }

    [Fact]
    public void SetSize_SameValue_DoesNotRaiseEvent()
    {
        var options = new ToolOptions();
        var raised = 0;
        options.Changed += (_, _) => raised++;

        options.SetSize(ToolKind.Pen, 3);

        Assert.Equal(0, raised);
    }

    [Theory]
    [InlineData(ToolKind.Pen, 2)]
    [InlineData(ToolKind.Dot, 50)]
    [InlineData(ToolKind.Eraser, 5)]
    public void SetSize_NonPresetValue_Throws(ToolKind tool, int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToolOptions().SetSize(tool, size));

    [Fact]
    public void GetSize_UnknownTool_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ToolOptions().GetSize((ToolKind)99));

    [Fact]
    public void Color_DefaultsToBlackAndRaisesOnChangeOnly()
    {
        var options = new ToolOptions();
        var raised = 0;
        options.Changed += (_, _) => raised++;

        Assert.Equal(RgbaColor.Black, options.Color);
        options.Color = RgbaColor.Black;
        options.Color = RgbaColor.White;

        Assert.Equal(1, raised);
        Assert.Equal(RgbaColor.White, options.Color);
    }
}
