namespace DrawingApp.Core.Application.Ports;

public sealed class ImageSaveException : Exception
{
    public ImageSaveException()
    {
    }

    public ImageSaveException(string message)
        : base(message)
    {
    }

    public ImageSaveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
