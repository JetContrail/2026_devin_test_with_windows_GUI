using System.IO;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Tests.Core;

/// <summary>Writes width, height and raw BGRA bytes so tests can inspect exactly what was saved.</summary>
internal sealed class FakeImageEncoder : IImageEncoder
{
    public string FormatId => "raw";

    public IReadOnlyList<string> Extensions { get; } = [".raw"];

    public static PixelBuffer Read(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        var image = new PixelBuffer(reader.ReadInt32(), reader.ReadInt32());
        reader.ReadBytes(image.GetBuffer().Length).CopyTo(image.GetBuffer(), 0);
        return image;
    }

    public void Encode(PixelBuffer image, Stream destination)
    {
        using var writer = new BinaryWriter(destination, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(image.Width);
        writer.Write(image.Height);
        writer.Write(image.GetBuffer());
    }
}
