using DrawingApp.Core.Domain.Viewport;

namespace DrawingApp.Tests.Core;

public class ZoomPolicyTests
{
    [Fact]
    public void Range_Is10To800PercentWith100PercentDefault()
    {
        Assert.Equal(0.10, ZoomPolicy.MinScale);
        Assert.Equal(8.00, ZoomPolicy.MaxScale);
        Assert.Equal(1.00, ZoomPolicy.DefaultScale);
        Assert.Contains(ZoomPolicy.DefaultScale, ZoomPolicy.Steps);
        Assert.Equal(ZoomPolicy.Steps.Order(), ZoomPolicy.Steps);
    }

    [Theory]
    [InlineData(1.00, 1, 1.25)]
    [InlineData(1.00, -1, 0.75)]
    [InlineData(1.00, 3, 2.00)]
    [InlineData(1.00, -2, 0.67)]
    [InlineData(1.00, 0, 1.00)]
    [InlineData(6.00, 5, 8.00)]
    [InlineData(8.00, 1, 8.00)]
    [InlineData(0.15, -5, 0.10)]
    [InlineData(0.10, -1, 0.10)]
    public void Step_MovesThroughTableAndStaysInRange(double current, int notches, double expected) =>
        Assert.Equal(expected, ZoomPolicy.Step(current, notches));

    [Theory]
    [InlineData(1.1, 1, 1.25)]
    [InlineData(1.1, -1, 1.00)]
    [InlineData(20, -1, 8.00)]
    [InlineData(0.01, 1, 0.10)]
    public void Step_FromOffTableScale_SnapsInRequestedDirection(double current, int notches, double expected) =>
        Assert.Equal(expected, ZoomPolicy.Step(current, notches));

    [Fact]
    public void Step_ManyNotches_EveryResultIsInRange()
    {
        var scale = ZoomPolicy.DefaultScale;
        for (var i = 0; i < 40; i++)
        {
            scale = ZoomPolicy.Step(scale, i % 7 < 4 ? 1 : -1);
            Assert.InRange(scale, ZoomPolicy.MinScale, ZoomPolicy.MaxScale);
        }
    }

    [Theory]
    [InlineData(0.01, 0.10)]
    [InlineData(1.10, 1.10)]
    [InlineData(99, 8.00)]
    public void Clamp_LimitsToRange(double scale, double expected) =>
        Assert.Equal(expected, ZoomPolicy.Clamp(scale));
}
