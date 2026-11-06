using System.Diagnostics.CodeAnalysis;
using System.Windows;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.UI.Views;

[ExcludeFromCodeCoverage(Justification = "XAML view; logic lives in MainViewModel.")]
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
