namespace ElgatoControl.Api.Services;

public interface ICameraController
{
    string? FindDevice();
    bool SetProperty(CameraProperty property, int value);
    string GetControls();
    void ResetToDefaults();
}
