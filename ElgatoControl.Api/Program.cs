using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ElgatoControl.Api.Services;
using ElgatoControl.Api.Endpoints;
using ElectronNET.API;
using ElectronNET.API.Entities;

var builder = WebApplication.CreateBuilder(args);

// Register camera service based on OS
#pragma warning disable CA1416
if (OperatingSystem.IsWindows())
    builder.Services.AddSingleton<ICameraDevice, WindowsCameraDevice>();
else
    builder.Services.AddSingleton<ICameraDevice, LinuxCameraDevice>();
#pragma warning restore CA1416

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Hook in Electron.NET
builder.WebHost.UseElectron(args);

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapCameraEndpoints();
app.MapPresetEndpoints();
app.MapStreamEndpoints();
app.MapScreenshotEndpoints();

// Enforce Preset A on boot
using (var scope = app.Services.CreateScope())
{
    var camera = scope.ServiceProvider.GetRequiredService<ICameraDevice>();
    string presetsFile = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "presets.json");

    if (System.IO.File.Exists(presetsFile))
    {
        try
        {
            var json = System.IO.File.ReadAllText(presetsFile);
            var presets = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, ElgatoControl.Api.Endpoints.PresetState>>(json);
            if (presets != null && presets.TryGetValue("A", out var stateA))
            {
                camera.SetProperty(CameraProperty.Zoom, stateA.Zoom);
                camera.SetProperty(CameraProperty.Pan, stateA.Pan);
                camera.SetProperty(CameraProperty.Tilt, stateA.Tilt);
            }
        }
        catch { }
    }
}

// Open Electron window (only runs when launched via electronize)
if (HybridSupport.IsElectronActive)
{
    await app.StartAsync();

    var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
    {
        Width = 1400,
        Height = 900,
        Title = "Elgato Camera Control",
        Show = false, // wait until ready to avoid blank flash
        WebPreferences = new WebPreferences
        {
            NodeIntegration = false,
            ContextIsolation = true
        }
    });

    window.OnReadyToShow += () => window.Show();
    window.OnClosed += () => Electron.App.Quit();

    await app.WaitForShutdownAsync();
}
else
{
    // Normal browser/API mode when not running via Electron
    app.Run("http://localhost:5000");
}
