using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ElgatoControl.Api.Services;
using ElgatoControl.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// SOLID: Register the appropriate hardware service based on OS
#pragma warning disable CA1416
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<ICameraDevice, WindowsCameraDevice>();
}
else
{
    builder.Services.AddSingleton<ICameraDevice, LinuxCameraDevice>();
}
#pragma warning restore CA1416

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Register all abstracted camera endpoints
app.MapCameraEndpoints();
app.MapPresetEndpoints();
app.MapStreamEndpoints();

// Enforce Preset A on Hardware Boot (Absolute Backend Truth)
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
        catch { } // Ignore JSON parse errors on initial boot
    }
}

app.Run("http://localhost:5000");
