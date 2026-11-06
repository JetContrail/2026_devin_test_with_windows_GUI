using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Core.Domain.Imaging;

namespace DrawingApp.Infrastructure.Imaging;

/// <summary>Decodes PNG/JPEG through WIC and normalizes to BGRA32, ignoring the file's DPI.</summary>
public sealed class WicImageDecoder : IImageDecoder
{
    /// <summary>4096 x 4096: keeps two layers plus their display bitmaps within the 300 MB memory budget.</summary>
    public const long DefaultMaxPixelCount = 4096L * 4096L;

    private readonly long _maxPixelCount;

    public WicImageDecoder()
        : this(DefaultMaxPixelCount)
    {
    }

    public WicImageDecoder(long maxPixelCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPixelCount);
        _maxPixelCount = maxPixelCount;
    }

    public PixelBuffer Decode(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        BitmapSource frame;
        try
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            frame = decoder.Frames[0];
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException or InvalidOperationException or IOException)
        {
            throw new ImageDecodeException("画像として読み込めないファイルです。", ex);
        }

        if ((long)frame.PixelWidth * frame.PixelHeight > _maxPixelCount)
        {
            throw new ImageDecodeException($"画像が大きすぎます ({frame.PixelWidth}x{frame.PixelHeight})。");
        }

        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var buffer = new PixelBuffer(converted.PixelWidth, converted.PixelHeight);
        converted.CopyPixels(buffer.GetBuffer(), buffer.Stride, 0);
        return buffer;
    }
}
