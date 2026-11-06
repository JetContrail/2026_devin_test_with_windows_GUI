using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Documents;

namespace DrawingApp.Core.Application.Services;

/// <summary>Document-level use cases (background loading; saving follows in Phase 5).</summary>
public sealed class DocumentService
{
    private readonly DrawingDocument _document;
    private readonly IImageDecoder _decoder;

    public DocumentService(DrawingDocument document, IImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(decoder);

        _document = document;
        _decoder = decoder;
    }

    /// <summary>Decodes the image and sets it as the background. The document is left unchanged on failure.</summary>
    /// <exception cref="ImageDecodeException">The file cannot be opened or is not a supported image.</exception>
    public void LoadBackground(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Stream stream;
        try
        {
            stream = File.OpenRead(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new ImageDecodeException($"ファイルを開けません: {path}", ex);
        }

        using (stream)
        {
            LoadBackground(stream);
        }
    }

    /// <inheritdoc cref="LoadBackground(string)"/>
    public void LoadBackground(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _document.LoadBackground(_decoder.Decode(stream));
    }
}
