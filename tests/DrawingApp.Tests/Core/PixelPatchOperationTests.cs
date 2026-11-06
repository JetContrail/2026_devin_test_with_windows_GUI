using DrawingApp.Core.Domain.History;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Layers;

namespace DrawingApp.Tests.Core;

public class PixelPatchOperationTests
{
    private readonly RasterLayer _layer = new("描画", new PixelBuffer(4, 4));
    private readonly PixelPatchOperation _operation;

    public PixelPatchOperationTests()
    {
        var area = new PixelRect(1, 1, 2, 2);
        var before = PixelPatch.Capture(_layer.Pixels, area);
        _layer.Pixels.SetPixel(1, 1, RgbaColor.Black);
        _layer.Pixels.SetPixel(2, 2, RgbaColor.Black);
        _operation = new PixelPatchOperation(_layer, before);
    }

    [Fact]
    public void Undo_RestoresPixelsBeforeEdit()
    {
        var area = _operation.Undo();

        Assert.Equal(new PixelRect(1, 1, 2, 2), area);
        Assert.All(_layer.Pixels.GetBuffer(), b => Assert.Equal(0, b));
    }

    [Fact]
    public void Redo_ReappliesEdit()
    {
        _operation.Undo();

        var area = _operation.Redo();

        Assert.Equal(new PixelRect(1, 1, 2, 2), area);
        Assert.Equal(RgbaColor.Black, _layer.Pixels.GetPixel(1, 1));
        Assert.Equal(RgbaColor.Black, _layer.Pixels.GetPixel(2, 2));
        Assert.Equal(RgbaColor.Transparent, _layer.Pixels.GetPixel(2, 1));
    }

    [Fact]
    public void UndoRedo_Repeated_KeepsAlternatingStates()
    {
        for (var i = 0; i < 5; i++)
        {
            _operation.Undo();
            Assert.Equal(RgbaColor.Transparent, _layer.Pixels.GetPixel(1, 1));
            _operation.Redo();
            Assert.Equal(RgbaColor.Black, _layer.Pixels.GetPixel(1, 1));
        }
    }

    [Fact]
    public void OutOfOrderCalls_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => _operation.Redo());
        _operation.Undo();
        Assert.Throws<InvalidOperationException>(() => _operation.Undo());
    }

    [Fact]
    public void SizeInBytes_IsCompressedPatchSize() =>
        Assert.InRange(_operation.SizeInBytes, 1, 2 * 2 * PixelBuffer.BytesPerPixel + 16);

    [Fact]
    public void Constructor_Null_Throws()
    {
        var patch = PixelPatch.Capture(_layer.Pixels, PixelRect.Empty);

        Assert.Throws<ArgumentNullException>(() => new PixelPatchOperation(null!, patch));
        Assert.Throws<ArgumentNullException>(() => new PixelPatchOperation(_layer, null!));
    }
}
