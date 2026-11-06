using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Core.Application.Services;

/// <summary>Saves the drawing layer alone or flattened with the background. The format is chosen from the file extension.</summary>
public sealed class ExportService
{
    private readonly DrawingDocument _document;

    public ExportService(DrawingDocument document, IEnumerable<IImageEncoder> encoders)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(encoders);

        _document = document;
        Encoders = [.. encoders];
    }

    public IReadOnlyList<IImageEncoder> Encoders { get; }

    /// <summary>Saves only the drawing layer; undrawn pixels stay transparent.</summary>
    /// <exception cref="ImageSaveException">Unsupported extension or the file cannot be written.</exception>
    public void SaveDrawingOnly(string path) => Save(_document.ActiveLayer.Pixels, path);

    /// <summary>Saves the visible layers flattened (drawing over background).</summary>
    /// <exception cref="ImageSaveException">Unsupported extension or the file cannot be written.</exception>
    public void SaveComposite(string path) => Save(LayerCompositor.Compose(_document.Layers), path);

    private void Save(PixelBuffer image, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var extension = Path.GetExtension(path);
        var encoder = Encoders.FirstOrDefault(e => e.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            ?? throw new ImageSaveException($"未対応の保存形式です: {extension}");

        // Write to a temporary file first so a failed save never leaves a truncated image behind.
        var temporary = path + ".tmp";
        try
        {
            using (var stream = File.Create(temporary))
            {
                encoder.Encode(image, stream);
            }

            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            TryDelete(temporary);
            throw new ImageSaveException($"ファイルを保存できません: {path}", ex);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: the original error is more useful to the caller.
        }
    }
}
