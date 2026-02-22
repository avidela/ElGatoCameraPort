namespace ElgatoControl.Api.Utilities;

public static class AppPaths
{
    // Stores user data (presets, config) in ~/.config/elgato-camera-control/
    // This is the XDG-standard location on Linux, and keeps dev + installed builds separate.
    public static string ConfigDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "elgato-camera-control");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string PresetsFile =>
        Path.Combine(ConfigDirectory, "presets.json");
}
