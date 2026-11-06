using DrawingApp.Core.Application.Ports;

namespace DrawingApp.UI.Services;

/// <summary>Modal dialogs used by view models, abstracted so they stay testable without WPF.</summary>
public interface IDialogService
{
    /// <summary>Asks for a PNG/JPG/JPEG file. Returns <see langword="null"/> when cancelled.</summary>
    string? ShowOpenImageDialog();

    /// <summary>Asks for a destination file; the file types offered come from <paramref name="encoders"/>. Returns <see langword="null"/> when cancelled.</summary>
    string? ShowSaveImageDialog(string title, string defaultFileName, IReadOnlyList<IImageEncoder> encoders);

    void ShowError(string message);
}
