namespace DrawingApp.UI.Services;

/// <summary>Modal dialogs used by view models, abstracted so they stay testable without WPF.</summary>
public interface IDialogService
{
    /// <summary>Asks for a PNG/JPG/JPEG file. Returns <see langword="null"/> when cancelled.</summary>
    string? ShowOpenImageDialog();

    void ShowError(string message);
}
