using DrawingApp.UI.Services;

namespace DrawingApp.Tests.UI;

internal sealed class FakeDialogService : IDialogService
{
    public string? OpenImagePath { get; set; }

    public List<string> Errors { get; } = [];

    public string? ShowOpenImageDialog() => OpenImagePath;

    public void ShowError(string message) => Errors.Add(message);
}
