using System.Diagnostics.CodeAnalysis;
using System.Windows;
using DrawingApp.Core.Application.Ports;
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

    public string? ShowSaveImageDialog(string title, string defaultFileName, IReadOnlyList<IImageEncoder> encoders)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = string.Join('|', encoders.Select(ToFilter)),
            AddExtension = true,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog(Application.Current?.MainWindow) == true ? dialog.FileName : null;
    }

    public void ShowError(string message) =>
        MessageBox.Show(Application.Current?.MainWindow!, message, "DrawingApp", MessageBoxButton.OK, MessageBoxImage.Error);

    private static string ToFilter(IImageEncoder encoder)
    {
        var patterns = string.Join(';', encoder.Extensions.Select(e => "*" + e));
        return $"{encoder.FormatId.ToUpperInvariant()} 画像 ({patterns})|{patterns}";
    }
}
