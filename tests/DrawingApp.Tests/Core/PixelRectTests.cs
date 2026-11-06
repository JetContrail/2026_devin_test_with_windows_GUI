using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public class PixelRectTests
{
    [Fact]
    public void Constructor_ExposesBounds()
    {
        var rect = new PixelRect(2, 3, 4, 5);

        Assert.Equal(6, rect.Right);
        Assert.Equal(8, rect.Bottom);
        Assert.False(rect.IsEmpty);
    }

    [Fact]
    public void Constructor_NegativeSize_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelRect(0, 0, -1, 1));

    [Fact]
    public void FromBounds_Inverted_ReturnsEmpty() =>
        Assert.True(PixelRect.FromBounds(5, 5, 5, 10).IsEmpty);

    [Fact]
    public void Union_CoversBothAndIgnoresEmpty()
    {
        var a = new PixelRect(0, 0, 2, 2);
        var b = new PixelRect(5, 1, 1, 4);

        Assert.Equal(new PixelRect(0, 0, 6, 5), a.Union(b));
        Assert.Equal(a, a.Union(PixelRect.Empty));
        Assert.Equal(b, PixelRect.Empty.Union(b));
    }

    [Fact]
    public void Intersect_ReturnsOverlapOrEmpty()
    {
        var a = new PixelRect(0, 0, 4, 4);

        Assert.Equal(new PixelRect(2, 2, 2, 2), a.Intersect(new PixelRect(2, 2, 10, 10)));
        Assert.True(a.Intersect(new PixelRect(10, 10, 1, 1)).IsEmpty);
    }
}
