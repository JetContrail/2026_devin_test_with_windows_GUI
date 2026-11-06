using System.Windows.Media;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.UI.Rendering;

namespace DrawingApp.Tests.UI;

public class LayerBitmapPresenterTests
{
    [Fact]
    public void New_CreatesBgra32BitmapWithSourceContent() => StaThread.Run(() =>
    {
        var source = new PixelBuffer(4, 3);
        source.SetPixel(1, 2, new RgbaColor(10, 20, 30, 40));

        var presenter = new LayerBitmapPresenter(source);

        Assert.Equal(4, presenter.Bitmap.PixelWidth);
        Assert.Equal(3, presenter.Bitmap.PixelHeight);
        Assert.Equal(PixelFormats.Bgra32, presenter.Bitmap.Format);
        Assert.Equal(new byte[] { 30, 20, 10, 40 }, ReadPixel(presenter, 1, 2));
    });

    [Fact]
    public void Update_CopiesOnlyTheGivenArea() => StaThread.Run(() =>
    {
        var source = new PixelBuffer(4, 4);
        var presenter = new LayerBitmapPresenter(source);
        source.SetPixel(0, 0, RgbaColor.Black);
        source.SetPixel(3, 3, RgbaColor.Black);

        presenter.Update(new PixelRect(2, 2, 10, 10));

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, ReadPixel(presenter, 0, 0));
        Assert.Equal(new byte[] { 0, 0, 0, 255 }, ReadPixel(presenter, 3, 3));
    });

    [Fact]
    public void Update_OutsideBounds_IsIgnored() => StaThread.Run(() =>
    {
        var presenter = new LayerBitmapPresenter(new PixelBuffer(2, 2));

        presenter.Update(new PixelRect(5, 5, 1, 1));

        Assert.Equal(new byte[] { 0, 0, 0, 0 }, ReadPixel(presenter, 1, 1));
    });

    [Fact]
    public void Constructor_Null_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new LayerBitmapPresenter(null!));

    private static byte[] ReadPixel(LayerBitmapPresenter presenter, int x, int y)
    {
        var pixel = new byte[4];
        presenter.Bitmap.CopyPixels(new System.Windows.Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return pixel;
    }
}
