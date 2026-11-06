using System.IO;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public sealed class DocumentServiceTests : IDisposable
{
    private readonly DrawingDocument _document = new();
    private readonly FakeImageDecoder _decoder = new(64, 32);
    private readonly DocumentService _service;
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");

    public DocumentServiceTests()
    {
        _service = new DocumentService(_document, _decoder);
    }

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void LoadBackground_FromFile_SetsBackgroundAndCanvasSize()
    {
        File.WriteAllBytes(_path, [1, 2, 3]);

        _service.LoadBackground(_path);

        Assert.Equal((64, 32), (_document.Width, _document.Height));
        Assert.Equal(new RgbaColor(10, 20, 30), _document.Background.Pixels.GetPixel(0, 0));
        Assert.Equal(RgbaColor.Transparent, _document.ActiveLayer.Pixels.GetPixel(0, 0));
        Assert.Equal(Path.GetFullPath(_path), _document.BackgroundPath);
    }

    [Fact]
    public void LoadBackground_KeepsExistingDrawing()
    {
        _document.ActiveLayer.Pixels.SetPixel(5, 5, RgbaColor.Black);

        _service.LoadBackground(new MemoryStream([1]));

        Assert.Equal(RgbaColor.Black, _document.ActiveLayer.Pixels.GetPixel(5, 5));
    }

    [Fact]
    public void LoadBackground_DecodeFailure_LeavesDocumentUnchanged()
    {
        _decoder.Fail = true;
        var background = _document.Background.Pixels;

        Assert.Throws<ImageDecodeException>(() => _service.LoadBackground(new MemoryStream([1])));

        Assert.Same(background, _document.Background.Pixels);
        Assert.Equal(DrawingDocument.DefaultWidth, _document.Width);
    }

    [Fact]
    public void LoadBackground_MissingFile_ThrowsImageDecodeException()
    {
        var ex = Assert.Throws<ImageDecodeException>(() => _service.LoadBackground(_path));

        Assert.IsType<FileNotFoundException>(ex.InnerException);
        Assert.Equal(0, _decoder.DecodeCount);
    }

    [Fact]
    public void ArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentService(null!, _decoder));
        Assert.Throws<ArgumentNullException>(() => new DocumentService(_document, null!));
        Assert.Throws<ArgumentException>(() => _service.LoadBackground(" "));
        Assert.Throws<ArgumentNullException>(() => _service.LoadBackground((Stream)null!));
    }
}
