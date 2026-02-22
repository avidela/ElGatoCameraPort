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
    private static Process? activeStreamProcess = null;

    public static void MapCameraEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Live Preview Stream ---
        app.MapGet("/api/camera/stream", async (HttpContext context, ICameraDevice camera, int? w, int? h, int? fps) =>
        {
            string? device = camera.FindDevice();
            if (device == null) return;

            // Use requested format or default to 1080p60
            int targetWidth = w ?? 1920;
            int targetHeight = h ?? 1080;
            int targetFps = fps ?? 60;

            var oldProcess = activeStreamProcess;
            activeStreamProcess = null;
            if (oldProcess != null)
            {
                try
                {
                    if (!oldProcess.HasExited) oldProcess.Kill();
                    oldProcess.Dispose();
                }
                catch { }
            }

            context.Response.ContentType = "multipart/x-mixed-replace; boundary=ffserver";

            string ffmpegPath = FindFFmpeg();
            // Use -c:v copy to pass the MJPEG stream directly without re-encoding, preserving pixel-perfect quality and reducing latency
            string args = OperatingSystem.IsWindows() 
                ? $"-f dshow -vcodec mjpeg -video_size {targetWidth}x{targetHeight} -framerate {targetFps} -i video=\"{device}\" -fflags nobuffer -c:v copy -f mpjpeg -boundary_tag ffserver -loglevel error -"
                : $"-f v4l2 -input_format mjpeg -video_size {targetWidth}x{targetHeight} -framerate {targetFps} -i {device} -fflags nobuffer -c:v copy -f mpjpeg -boundary_tag ffserver -loglevel error -";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            activeStreamProcess = Process.Start(psi);
            if (activeStreamProcess == null) return;

            try
            {
                // Custom manual chunk loop to instantly flush frames to the browser instead of letting ASP.NET Core buffer them
                byte[] buffer = new byte[81920]; 
                int bytesRead;
                var baseStream = activeStreamProcess.StandardOutput.BaseStream;

                while ((bytesRead = await baseStream.ReadAsync(buffer, 0, buffer.Length, context.RequestAborted)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, bytesRead, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
            }
            catch (OperationCanceledException) { }
        });

        app.MapPost("/api/camera/stream/stop", () =>
        {
            var p = activeStreamProcess;
            activeStreamProcess = null;
            if (p != null)
            {
                try
                {
                    if (!p.HasExited) p.Kill();
                    p.Dispose();
                }
                catch { }
            }
            return Results.Json(new { success = true });
        });

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
            return Results.Json(new { success = true, raw = camera.GetControls() });
        });

        app.MapGet("/api/camera/formats", (ICameraDevice camera) => {
            return Results.Json(new { success = true, formats = camera.GetSupportedFormats() });
        });

        app.MapGet("/api/camera/layout", (ICameraDevice camera) => {
            return Results.Json(new { success = true, layout = camera.GetLayout() });
        });

        string presetsFile = Path.Combine(Directory.GetCurrentDirectory(), "presets.json");

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

    private static string FindFFmpeg()
    {
        if (OperatingSystem.IsWindows())
        {
            string wingetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                @"Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.0.1-full_build\bin\ffmpeg.exe");
            
            if (File.Exists(wingetPath)) return wingetPath;
            return "ffmpeg.exe";
        }
        return "ffmpeg";
    }
}

public record CameraSetting(string Prop, int Val);
public record PresetState(int Zoom, int Pan, int Tilt);
public record PresetSaveRequest(int Zoom, int Pan, int Tilt);
