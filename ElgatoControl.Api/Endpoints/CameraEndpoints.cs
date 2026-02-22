using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using ElgatoControl.Api.Models;
using ElgatoControl.Api.Services;

namespace ElgatoControl.Api.Endpoints;

public static class CameraEndpoints
{
    public static void MapCameraEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Settings API (POST) ---
        app.MapPost("/api/camera/set", (CameraSetting setting, ICameraDevice camera) =>
        {
            if (!Enum.TryParse<CameraProperty>(setting.Prop, true, out var prop))
            {
                return Results.Json(new { success = false, message = "Invalid property" });
            }

            bool success = camera.SetProperty(prop, setting.Val);
            return Results.Json(new { success });
        });

        app.MapPost("/api/camera/reset", (ICameraDevice camera) =>
        {
            camera.ResetToDefaults();
            return Results.Json(new { success = true });
        });

        app.MapPost("/api/camera/save", (ICameraDevice camera) =>
        {
            return Results.Json(new { success = true, message = "Settings updated in hardware registers." });
        });

        app.MapGet("/api/camera/controls", (ICameraDevice camera) => {
            return Results.Json(new { success = true, values = camera.GetControlValues() });
        });

        app.MapGet("/api/camera/formats", (ICameraDevice camera) => {
            return Results.Json(new { success = true, formats = camera.GetSupportedFormats() });
        });

        app.MapGet("/api/camera/layout", (ICameraDevice camera) => {
            return Results.Json(new { success = true, layout = camera.GetLayout() });
        });
    }
}

public record CameraSetting(string Prop, int Val);
