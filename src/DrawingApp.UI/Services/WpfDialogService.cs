using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.Win32;

namespace DrawingApp.UI.Services;

[ExcludeFromCodeCoverage(Justification = "Thin wrapper over modal WPF dialogs.")]
public sealed class WpfDialogService : IDialogService
{
    public string? ShowOpenImageDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "下絵を開く",
            Filter = "画像ファイル (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
        };

        return dialog.ShowDialog(Application.Current?.MainWindow) == true ? dialog.FileName : null;
    }

    public void ShowError(string message) =>
        MessageBox.Show(Application.Current?.MainWindow!, message, "DrawingApp", MessageBoxButton.OK, MessageBoxImage.Error);
}
