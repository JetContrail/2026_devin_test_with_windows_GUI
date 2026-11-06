namespace DrawingApp.Core.Application.AutoSave;

/// <summary>Metadata stored next to the drawing layer image in each generation (manifest.json).</summary>
public sealed record AutoSaveManifest(
    DateTimeOffset SavedAt,
    int Width,
    int Height,
    string DrawingFile,
    string? BackgroundPath);
