using System.Diagnostics.CodeAnalysis;

namespace DrawingApp.Core.Domain.Layers;

/// <summary>Layers ordered bottom to top: the background first, then raster layers.</summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Domain term: a stack of layers, not a Stack<T> collection.")]
public sealed class LayerStack
{
    private readonly List<Layer> _layers;

    public LayerStack(BackgroundLayer background, RasterLayer drawing)
    {
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(drawing);

        Background = background;
        ActiveLayer = drawing;
        _layers = [background, drawing];
    }

    public BackgroundLayer Background { get; }

    /// <summary>The layer tools edit. Always editable, so the background can never be modified by a tool.</summary>
    public RasterLayer ActiveLayer { get; }

    public IReadOnlyList<Layer> Layers => _layers;
}
