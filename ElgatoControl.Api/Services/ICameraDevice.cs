using System.Collections.Generic;
using ElgatoControl.Api.Models;

namespace ElgatoControl.Api.Services;

public interface ICameraDevice
{
    string? FindDevice();
    bool SetProperty(CameraProperty property, int value);
    string GetControls();
    Dictionary<string, int> GetControlValues();
    void ResetToDefaults();
    IEnumerable<VideoFormat> GetSupportedFormats();
    IEnumerable<ControlSectionData> GetLayout();
}
