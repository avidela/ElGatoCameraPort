using CommunityToolkit.Mvvm.ComponentModel;
using ElgatoControl.Core.Models;
using ElgatoControl.Core.Services;
using System;

namespace ElgatoControl.Avalonia.ViewModels;

public partial class ControlViewModel : ObservableObject
{
    private readonly ICameraDevice _cameraDevice;
    private readonly CameraControl _model;
    private bool _isUpdating;

    [ObservableProperty]
    private int _value;

    public string Label => _model.Label;
    public int Min => _model.Min;
    public int Max => _model.Max;
    public int Step => _model.Step;
    public string? Unit => _model.Unit;
    public string Id => _model.Id;

    public ControlViewModel(CameraControl model, ICameraDevice cameraDevice)
    {
        _model = model;
        _cameraDevice = cameraDevice;
        // Do not set Value here to trigger unnecessary updates, use UpdateValue for initial sync
        _value = model.DefaultValue;
    }

    partial void OnValueChanged(int value)
    {
        if (_isUpdating) return;

        // Map ID to CameraProperty
        if (TryMapProperty(Id, out var property))
        {
            _cameraDevice.SetProperty(property, value);
        }
    }

    public void UpdateValue(int newValue)
    {
        _isUpdating = true;
        Value = newValue;
        _isUpdating = false;
    }

    private bool TryMapProperty(string id, out CameraProperty property)
    {
        if (string.Equals(id, "white_balance", StringComparison.OrdinalIgnoreCase))
        {
            property = CameraProperty.WhiteBalance;
            return true;
        }

        if (Enum.TryParse<CameraProperty>(id, true, out property))
        {
            return true;
        }

        return false;
    }
}
