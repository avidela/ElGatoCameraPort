using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ElgatoControl.Api.Services;

public class LinuxCameraController : ICameraController
{
    public string? FindDevice()
    {
        // Primary: Try v4l2-ctl
        string output = RunCommand("v4l2-ctl", "--list-devices");
        if (!output.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool foundElgato = false;
            foreach (var line in lines)
            {
                if (line.Contains("Elgato Facecam", StringComparison.OrdinalIgnoreCase))
                {
                    foundElgato = true;
                    continue;
                }

                if (foundElgato)
                {
                    var match = Regex.Match(line, @"/dev/video\d+");
                    if (match.Success) return match.Value;
                }
            }
        }

        // Fallback: Scan /sys/class/video4linux/ directly
        try
        {
            if (Directory.Exists("/sys/class/video4linux"))
            {
                var devices = Directory.GetDirectories("/sys/class/video4linux");
                foreach (var devPath in devices)
                {
                    string nameFile = Path.Combine(devPath, "name");
                    if (File.Exists(nameFile))
                    {
                        string name = File.ReadAllText(nameFile).Trim();
                        if (name.Contains("Elgato Facecam", StringComparison.OrdinalIgnoreCase))
                        {
                            return $"/dev/{Path.GetFileName(devPath)}";
                        }
                    }
                }
            }
        }
        catch { /* Fallback failed */ }

        return null;
    }

    public bool SetProperty(CameraProperty property, int value)
    {
        string? device = FindDevice();
        if (device == null) return false;

        // Auto-switch to manual mode when adjusting specific properties
        if (property == CameraProperty.Exposure)
        {
            // Set auto_exposure to Manual Mode (1)
            RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=auto_exposure=1");
        }
        else if (property == CameraProperty.WhiteBalance)
        {
            // Set white_balance_automatic to Disabled (0)
            RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=white_balance_automatic=0");
        }

        string mappedProp = MapProperty(property);
        string result = RunCommand("v4l2-ctl", $"-d {device} --set-ctrl={mappedProp}={value}");
        
        return string.IsNullOrEmpty(result) || !result.Contains("error", StringComparison.OrdinalIgnoreCase);
    }

    public string GetControls()
    {
        string? device = FindDevice();
        if (device == null) return "Camera not found";
        return RunCommand("v4l2-ctl", $"-d {device} -L");
    }

    public void ResetToDefaults()
    {
        string? device = FindDevice();
        if (device == null) return;

        // Reset all supported controls to their defaults
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=brightness=0");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=contrast=80");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=saturation=64");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=sharpness=128");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=gain=0");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=white_balance_automatic=1");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=auto_exposure=3"); // Aperture Priority Mode
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=zoom_absolute=100");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=pan_absolute=0");
        RunCommand("v4l2-ctl", $"-d {device} --set-ctrl=tilt_absolute=0");
    }

    private string MapProperty(CameraProperty property)
    {
        return property switch
        {
            CameraProperty.Zoom => "zoom_absolute",
            CameraProperty.Exposure => "exposure_time_absolute",
            CameraProperty.Gain => "gain",
            CameraProperty.WhiteBalance => "white_balance_temperature",
            CameraProperty.Brightness => "brightness",
            CameraProperty.Contrast => "contrast",
            CameraProperty.Saturation => "saturation",
            CameraProperty.Sharpness => "sharpness",
            CameraProperty.Pan => "pan_absolute",
            CameraProperty.Tilt => "tilt_absolute",
            _ => property.ToString().ToLower()
        };
    }

    private string RunCommand(string cmd, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "Error: Failed to start process";
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrEmpty(error) ? output : $"Error: {error}";
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return $"Error: Command '{cmd}' not found. Please ensure it is installed.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
