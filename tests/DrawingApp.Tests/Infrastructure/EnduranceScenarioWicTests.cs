using System.IO;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Infrastructure.Imaging;
using DrawingApp.Tests.Core;
using DrawingApp.Tests.UI;

namespace DrawingApp.Tests.Infrastructure;

[Collection(EnduranceTestGroup.Name)]
public sealed class EnduranceScenarioWicTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("endurance-wic-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Repeat100Times_WithWicPng_Completes() => StaThread.Run(() =>
    {
        var encoder = new PngImageEncoder();
        var background = Path.Combine(_directory, "background.png");
        var image = new PixelBuffer(256, 192);
        image.Fill(new RgbaColor(200, 220, 240));
        using (var stream = File.Create(background))
        {
            encoder.Encode(image, stream);
        }

        EnduranceScenario.Run(encoder, new WicImageDecoder(), background, _directory);
    });
}
