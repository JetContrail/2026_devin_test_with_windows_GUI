using DrawingApp.Core.Application.Ports;
using DrawingApp.UI.Services;

namespace DrawingApp.Tests.UI;

internal sealed class FakeDialogService : IDialogService
{
    public string? OpenImagePath { get; set; }

    public string? SaveImagePath { get; set; }

    public List<string> Errors { get; } = [];

    public List<(string Title, string DefaultFileName, IReadOnlyList<IImageEncoder> Encoders)> SaveRequests { get; } = [];

    public string? ShowOpenImageDialog() => OpenImagePath;

    public string? ShowSaveImageDialog(string title, string defaultFileName, IReadOnlyList<IImageEncoder> encoders)
    {
        SaveRequests.Add((title, defaultFileName, encoders));
        return SaveImagePath;
    }

    public void ShowError(string message) => Errors.Add(message);
}
