namespace DrawingApp.Core.Domain.Rasterization;

public enum BlendMode
{
    /// <summary>Replaces covered pixels with the brush color.</summary>
    Paint,

    /// <summary>Makes covered pixels fully transparent.</summary>
    Erase,
}
