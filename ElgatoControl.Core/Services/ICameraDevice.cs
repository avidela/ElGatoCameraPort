using System.Collections.Generic;
using ElgatoControl.Core.Models;

namespace ElgatoControl.Core.Services;

public interface ICameraDevice
{
    string? FindDevice();
    string DeviceName { get; }
    bool SetProperty(CameraProperty property, int value);
    string GetControls();
    Dictionary<string, int> GetControlValues();
    void ResetToDefaults();
    IEnumerable<VideoFormat> GetSupportedFormats();
    IEnumerable<ControlSectionData> GetLayout();
}
