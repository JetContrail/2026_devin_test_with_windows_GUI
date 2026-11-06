namespace DrawingApp.Core.Application.Ports;

public sealed class ImageDecodeException : Exception
{
    public ImageDecodeException()
    {
    }

    public ImageDecodeException(string message)
        : base(message)
    {
    }

    public ImageDecodeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
