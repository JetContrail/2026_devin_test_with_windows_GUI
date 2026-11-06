namespace DrawingApp.Core.Domain.Imaging;

/// <summary>Position in canvas pixel coordinates (origin at the top-left corner of the canvas).</summary>
public readonly record struct CanvasPoint(double X, double Y)
{
    public double DistanceTo(CanvasPoint other) => Math.Sqrt(((X - other.X) * (X - other.X)) + ((Y - other.Y) * (Y - other.Y)));
}
