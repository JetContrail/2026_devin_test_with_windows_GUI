namespace DrawingApp.Core.Application.AutoSave;

public sealed class AutoSaveOptions
{
    public const int DefaultMaxGenerations = 10;

    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(60);

    public TimeSpan Interval { get; init; } = DefaultInterval;

    /// <summary>Number of generations kept; older ones are deleted after each save.</summary>
    public int MaxGenerations { get; init; } = DefaultMaxGenerations;

    /// <summary><see cref="Ports.IImageEncoder.FormatId"/> used for the drawing layer.</summary>
    public string ImageFormatId { get; init; } = "png";
}
