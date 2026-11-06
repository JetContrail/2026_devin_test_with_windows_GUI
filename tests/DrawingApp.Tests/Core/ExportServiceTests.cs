using System.IO;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

public sealed class ExportServiceTests : IDisposable
{
    private readonly DrawingDocument _document = new(8, 4);
    private readonly ExportService _service;
    private readonly string _directory = Directory.CreateTempSubdirectory("drawingapp-export-").FullName;

    public ExportServiceTests()
    {
        _service = new ExportService(_document, [new FakeImageEncoder()]);
        _document.ActiveLayer.Pixels.SetPixel(1, 1, RgbaColor.Black);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void SaveDrawingOnly_WritesDrawingLayerWithoutBackground()
    {
        var path = Path.Combine(_directory, "drawing.raw");

        _service.SaveDrawingOnly(path);

        var saved = FakeImageEncoder.Read(path);
        Assert.Equal(_document.ActiveLayer.Pixels.GetBuffer(), saved.GetBuffer());
        Assert.Equal(RgbaColor.Black, saved.GetPixel(1, 1));
        Assert.Equal(RgbaColor.Transparent, saved.GetPixel(0, 0));
    }

    [Fact]
    public void SaveComposite_WritesDrawingOverBackground()
    {
        var path = Path.Combine(_directory, "composite.RAW");

        _service.SaveComposite(path);

        var saved = FakeImageEncoder.Read(path);
        Assert.Equal(RgbaColor.Black, saved.GetPixel(1, 1));
        Assert.Equal(RgbaColor.White, saved.GetPixel(0, 0));
    }

    [Fact]
    public void Save_OverwritesExistingFileAndLeavesNoTemporaryFile()
    {
        var path = Path.Combine(_directory, "drawing.raw");
        File.WriteAllText(path, "old");

        _service.SaveDrawingOnly(path);

        Assert.Equal(8, FakeImageEncoder.Read(path).Width);
        Assert.Equal(["drawing.raw"], Directory.GetFiles(_directory).Select(Path.GetFileName));
    }

    [Fact]
    public void Save_UnsupportedExtension_Throws()
    {
        var ex = Assert.Throws<ImageSaveException>(() => _service.SaveDrawingOnly(Path.Combine(_directory, "a.bmp")));

        Assert.Contains(".bmp", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_MissingDirectory_ThrowsImageSaveException()
    {
        var path = Path.Combine(_directory, "missing", "a.raw");

        var ex = Assert.Throws<ImageSaveException>(() => _service.SaveComposite(path));

        Assert.IsAssignableFrom<IOException>(ex.InnerException);
    }

    [Fact]
    public void ArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new ExportService(null!, []));
        Assert.Throws<ArgumentNullException>(() => new ExportService(_document, null!));
        Assert.Throws<ArgumentException>(() => _service.SaveDrawingOnly(""));
        Assert.Single(_service.Encoders);
    }
}
