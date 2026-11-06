using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Viewport;

namespace DrawingApp.Tests.Core;

public class ViewportStateTests
{
    private readonly ViewportState _viewport = new();
    private int _changed;

    public ViewportStateTests() => _viewport.Changed += (_, _) => _changed++;

    [Fact]
    public void New_IsIdentityAt100Percent()
    {
        Assert.Equal(1.0, _viewport.Scale);
        Assert.Equal((0.0, 0.0), (_viewport.OffsetX, _viewport.OffsetY));
        Assert.Equal(new CanvasPoint(12, 34), _viewport.ScreenToCanvas(12, 34));
    }

    [Theory]
    [InlineData(100, 50, 1)]
    [InlineData(100, 50, -1)]
    [InlineData(0, 0, 4)]
    [InlineData(640, 360, -6)]
    [InlineData(333.5, 12.25, 20)]
    public void ZoomAt_KeepsCanvasPointUnderCursor(double x, double y, int notches)
    {
        _viewport.PanBy(-37, 21);
        var before = _viewport.ScreenToCanvas(x, y);

        _viewport.ZoomAt(x, y, notches);

        var after = _viewport.ScreenToCanvas(x, y);
        Assert.Equal(before.X, after.X, 9);
        Assert.Equal(before.Y, after.Y, 9);
    }

    [Fact]
    public void ZoomAt_ChangesScaleByOneStepAndRaisesChanged()
    {
        _viewport.ZoomAt(10, 10, 1);

        Assert.Equal(1.25, _viewport.Scale);
        Assert.Equal(1, _changed);
    }

    [Fact]
    public void ZoomAt_AtLimit_DoesNothing()
    {
        _viewport.ZoomAt(10, 10, 100);
        _changed = 0;
        var offset = (_viewport.OffsetX, _viewport.OffsetY);

        _viewport.ZoomAt(50, 50, 1);

        Assert.Equal(ZoomPolicy.MaxScale, _viewport.Scale);
        Assert.Equal(offset, (_viewport.OffsetX, _viewport.OffsetY));
        Assert.Equal(0, _changed);
    }

    [Fact]
    public void PanBy_MovesOffsetAndScreenToCanvas()
    {
        _viewport.ZoomAt(0, 0, 3); // 200%
        _viewport.PanBy(20, -10);

        Assert.Equal((20.0, -10.0), (_viewport.OffsetX, _viewport.OffsetY));
        Assert.Equal(new CanvasPoint(15, 20), _viewport.ScreenToCanvas(50, 30));
        Assert.Equal(2, _changed);
    }

    [Fact]
    public void PanBy_Zero_DoesNotRaiseChanged()
    {
        _viewport.PanBy(0, 0);

        Assert.Equal(0, _changed);
    }

    [Fact]
    public void Reset_ReturnsToIdentity()
    {
        _viewport.ZoomAt(100, 100, 2);
        _viewport.PanBy(5, 5);

        _viewport.Reset();

        Assert.Equal(1.0, _viewport.Scale);
        Assert.Equal((0.0, 0.0), (_viewport.OffsetX, _viewport.OffsetY));
        Assert.Equal(3, _changed);
    }
}
