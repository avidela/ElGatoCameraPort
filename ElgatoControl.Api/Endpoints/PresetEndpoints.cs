using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ElgatoControl.Core.Models;
using ElgatoControl.Core.Services;
using ElgatoControl.Core.Utilities;

namespace ElgatoControl.Api.Endpoints;

public static class PresetEndpoints
{
    public static void MapPresetEndpoints(this IEndpointRouteBuilder app)
    {
        string presetsFile = AppPaths.PresetsFile;

        app.MapPost("/api/camera/preset/save/{id}", async (string id, PresetSaveRequest req, ICameraDevice camera) =>
        {
            var presets = new System.Collections.Generic.Dictionary<string, PresetState>();
            if (File.Exists(presetsFile))
            {
                var json = await File.ReadAllTextAsync(presetsFile);
                presets = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, PresetState>>(json) ?? new();
            }
            presets[id] = new PresetState(req.Zoom, req.Pan, req.Tilt);
            await File.WriteAllTextAsync(presetsFile, System.Text.Json.JsonSerializer.Serialize(presets));
            return Results.Json(new { success = true });
        });

        app.MapGet("/api/camera/preset/load/{id}", async (string id, ICameraDevice camera) =>
        {
            if (!File.Exists(presetsFile)) return Results.Json(new { success = false, message = "No presets saved" });
            
            var json = await File.ReadAllTextAsync(presetsFile);
            var presets = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, PresetState>>(json);
            
            if (presets != null && presets.TryGetValue(id, out var state))
            {
                camera.SetProperty(CameraProperty.Zoom, state.Zoom);
                camera.SetProperty(CameraProperty.Pan, state.Pan);
                camera.SetProperty(CameraProperty.Tilt, state.Tilt);
                return Results.Json(new { success = true, state });
            }
            return Results.Json(new { success = false, message = "Preset not found" });
        });
    }
}

public record PresetState(int Zoom, int Pan, int Tilt);
public record PresetSaveRequest(int Zoom, int Pan, int Tilt);
