using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Layers;

namespace DrawingApp.Core.Domain.Documents;

/// <summary>Canvas made of a background layer and a drawing layer of the same size.</summary>
public sealed class DrawingDocument
{
    public const int DefaultWidth = 1280;
    public const int DefaultHeight = 720;

    public DrawingDocument()
        : this(DefaultWidth, DefaultHeight)
    {
    }

    public DrawingDocument(int width, int height)
    {
        var background = new PixelBuffer(width, height);
        background.Fill(RgbaColor.White);
        Layers = new LayerStack(new BackgroundLayer(background), new RasterLayer("描画", new PixelBuffer(width, height)));
    }

    /// <summary>Raised when the canvas size or the layer buffers are replaced (e.g. a background image was loaded).</summary>
    public event EventHandler? Reset;

    public LayerStack Layers { get; }

    public BackgroundLayer Background => Layers.Background;

    public RasterLayer ActiveLayer => Layers.ActiveLayer;

    public int Width => Background.Pixels.Width;

    public int Height => Background.Pixels.Height;

    /// <summary>File the background was loaded from, or null for the initial blank background.</summary>
    public string? BackgroundPath { get; private set; }

    /// <summary>
    /// Sets the background image and resizes the canvas to it. The existing drawing is kept anchored
    /// at the top-left corner; anything outside the new size is cropped.
    /// </summary>
    public void LoadBackground(PixelBuffer image, string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(image);

        ActiveLayer.ReplacePixels(ActiveLayer.Pixels.Resized(image.Width, image.Height));
        Background.ReplacePixels(image);
        BackgroundPath = sourcePath;
        Reset?.Invoke(this, EventArgs.Empty);
    }
}
