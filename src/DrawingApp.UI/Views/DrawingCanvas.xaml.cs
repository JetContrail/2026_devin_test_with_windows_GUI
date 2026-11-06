using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrawingApp.Core.Application.Services;
using DrawingApp.UI.Rendering;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.UI.Views;

/// <summary>Displays the layers and translates mouse input into canvas-pixel pointer events.</summary>
[ExcludeFromCodeCoverage(Justification = "XAML view; logic lives in CanvasViewModel and LayerBitmapPresenter.")]
public partial class DrawingCanvas : UserControl
{
    private CanvasViewModel? _viewModel;
    private LayerBitmapPresenter? _presenter;

    public DrawingCanvas()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.Invalidated -= OnInvalidated;
        }

        _viewModel = e.NewValue as CanvasViewModel;
        _presenter = _viewModel is null ? null : new LayerBitmapPresenter(_viewModel.DrawingLayer);
        DrawingImage.Source = _presenter?.Bitmap;

        if (_viewModel is not null)
        {
            _viewModel.Invalidated += OnInvalidated;
        }
    }

    private void OnInvalidated(object? sender, CanvasInvalidatedEventArgs e) => _presenter?.Update(e.Area);

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is null || !Surface.CaptureMouse())
        {
            return;
        }

        var p = e.GetPosition(Surface);
        _viewModel.PointerDown(p.X, p.Y);
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel is { IsStrokeActive: true })
        {
            var p = e.GetPosition(Surface);
            _viewModel.PointerMove(p.X, p.Y);
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is { IsStrokeActive: true })
        {
            var p = e.GetPosition(Surface);
            _viewModel.PointerUp(p.X, p.Y);
            e.Handled = true;
        }

        Surface.ReleaseMouseCapture();
    }

    private void OnLostMouseCapture(object sender, MouseEventArgs e) => _viewModel?.CancelPointer();
}
