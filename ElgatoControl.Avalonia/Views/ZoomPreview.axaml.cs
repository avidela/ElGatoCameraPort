using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ElgatoControl.Avalonia.ViewModels;
using System;

namespace ElgatoControl.Avalonia.Views;

public partial class ZoomPreview : UserControl
{
    private bool _isDragging;
    private Border? _zoomBox;
    private Canvas? _sensorCanvas;

    public ZoomPreview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _zoomBox = this.FindControl<Border>("ZoomBox");
        _sensorCanvas = this.FindControl<Canvas>("SensorCanvas");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            if (vm.ZoomVm != null) vm.ZoomVm.PropertyChanged += OnViewModelPropertyChanged;
            if (vm.PanVm != null) vm.PanVm.PropertyChanged += OnViewModelPropertyChanged;
            if (vm.TiltVm != null) vm.TiltVm.PropertyChanged += OnViewModelPropertyChanged;
        }
        UpdateBoxFromValues();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            if (vm.ZoomVm != null) vm.ZoomVm.PropertyChanged -= OnViewModelPropertyChanged;
            if (vm.PanVm != null) vm.PanVm.PropertyChanged -= OnViewModelPropertyChanged;
            if (vm.TiltVm != null) vm.TiltVm.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // For simplicity, re-render on any property change that might affect layout
        UpdateBoxFromValues();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty)
        {
            UpdateBoxFromValues();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        UpdatePanTiltFromMouse(e.GetPosition(_sensorCanvas));
        e.Pointer.Capture(_sensorCanvas);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            UpdatePanTiltFromMouse(e.GetPosition(_sensorCanvas));
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void UpdatePanTiltFromMouse(Point pos)
    {
        if (_sensorCanvas == null || DataContext is not MainViewModel vm || vm.ZoomVm == null) return;

        double zoom = vm.ZoomVm.Value;
        double sizePerc = 100.0 / (zoom / 100.0);
        if (sizePerc >= 100) return;

        // Calculate click position as percentage of container (-50 to 50)
        double xPerc = (pos.X / _sensorCanvas.Bounds.Width) * 100.0 - 50.0;
        double yPerc = (pos.Y / _sensorCanvas.Bounds.Height) * 100.0 - 50.0;

        // Clamp to allowed travel
        double maxTravel = (100.0 - sizePerc) / 2.0;
        xPerc = Math.Clamp(xPerc, -maxTravel, maxTravel);
        yPerc = Math.Clamp(yPerc, -maxTravel, maxTravel);

        // Map back to -100 to 100
        int newPan = (int)Math.Round((xPerc / maxTravel) * 100.0);
        int newTilt = (int)Math.Round(-(yPerc / maxTravel) * 100.0);

        if (vm.PanVm != null) vm.PanVm.Value = newPan;
        if (vm.TiltVm != null) vm.TiltVm.Value = newTilt;

        UpdateBoxFromValues();
    }

    private void UpdateBoxFromValues()
    {
        if (_zoomBox == null || _sensorCanvas == null || DataContext is not MainViewModel vm || vm.ZoomVm == null) return;

        double zoom = vm.ZoomVm.Value;
        double pan = vm.PanVm?.Value ?? 0;
        double tilt = vm.TiltVm?.Value ?? 0;

        double sizePerc = 100.0 / (zoom / 100.0);
        double maxTravel = (100.0 - sizePerc) / 2.0;

        double panPerc = (pan / 100.0) * maxTravel;
        double tiltPerc = -(tilt / 100.0) * maxTravel;

        _zoomBox.Width = (sizePerc / 100.0) * _sensorCanvas.Bounds.Width;
        _zoomBox.Height = (sizePerc / 100.0) * _sensorCanvas.Bounds.Height;

        _zoomBox.Margin = new Thickness(
            (50.0 + panPerc - sizePerc / 2.0) / 100.0 * _sensorCanvas.Bounds.Width,
            (50.0 + tiltPerc - sizePerc / 2.0) / 100.0 * _sensorCanvas.Bounds.Height,
            0, 0
        );
    }
}
