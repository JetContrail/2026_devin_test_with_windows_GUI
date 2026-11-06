using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrawingApp.Core.Application.Services;
using DrawingApp.UI.Rendering;
using DrawingApp.UI.ViewModels;

namespace DrawingApp.UI.Views;

/// <summary>
/// Displays the layers through the zoom/pan transform and translates mouse input into canvas-pixel pointer events,
/// wheel zoom, and middle-button or Space+left-button panning.
/// </summary>
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
            _viewModel.LayersReset -= OnLayersReset;
        }

        _viewModel = e.NewValue as CanvasViewModel;
        RecreateBitmaps();

        if (_viewModel is not null)
        {
            _viewModel.Invalidated += OnInvalidated;
            _viewModel.LayersReset += OnLayersReset;
        }
    }

    private void OnLayersReset(object? sender, EventArgs e) => RecreateBitmaps();

    private void RecreateBitmaps()
    {
        // The background never changes after loading, so it is converted once without dirty tracking.
        BackgroundImage.Source = _viewModel is null ? null : new LayerBitmapPresenter(_viewModel.BackgroundLayer).Bitmap;
        _presenter = _viewModel is null ? null : new LayerBitmapPresenter(_viewModel.DrawingLayer);
        DrawingImage.Source = _presenter?.Bitmap;
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

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Keep Space away from toolbar buttons, which would otherwise be "clicked" by it.
        Focus();
        if (Keyboard.IsKeyDown(Key.Space) && StartPan(e))
        {
            e.Handled = true;
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && StartPan(e))
        {
            e.Handled = true;
        }
    }

    private void OnViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel is { IsPanning: true })
        {
            var p = e.GetPosition(Viewport);
            _viewModel.PanTo(p.X, p.Y);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel is { IsPanning: true } && e.ChangedButton is MouseButton.Middle or MouseButton.Left)
        {
            EndPan();
            e.Handled = true;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var p = e.GetPosition(Viewport);
        _viewModel.Zoom(p.X, p.Y, e.Delta);
        e.Handled = true;
    }

    private void OnViewportLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_viewModel is { IsPanning: true } && !IsMouseCaptured)
        {
            EndPan();
        }
    }

    private bool StartPan(MouseButtonEventArgs e)
    {
        var p = e.GetPosition(Viewport);
        if (_viewModel is null || !_viewModel.BeginPan(p.X, p.Y))
        {
            return false;
        }

        if (!CaptureMouse())
        {
            _viewModel.EndPan();
            return false;
        }

        Cursor = Cursors.SizeAll;
        return true;
    }

    private void EndPan()
    {
        _viewModel?.EndPan();
        ClearValue(CursorProperty);
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }
}
