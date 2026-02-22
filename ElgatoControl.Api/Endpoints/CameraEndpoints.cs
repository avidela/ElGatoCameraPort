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
        app.MapGet("/api/camera/stream", async (HttpContext context, ICameraController camera, int? w, int? h, int? fps) =>
        {
            string? device = camera.FindDevice();
            if (device == null) return;

            // Use requested format or default to 1080p60
            int targetWidth = w ?? 1920;
            int targetHeight = h ?? 1080;
            int targetFps = fps ?? 60;

            if (activeStreamProcess != null && !activeStreamProcess.HasExited)
            {
                activeStreamProcess.Kill();
                activeStreamProcess.Dispose();
                activeStreamProcess = null;
            }

            context.Response.ContentType = "multipart/x-mixed-replace; boundary=ffserver";

            string ffmpegPath = FindFFmpeg();
            string args = OperatingSystem.IsWindows() 
                ? $"-f dshow -i video=\"{device}\" -f mpjpeg -boundary_tag ffserver -video_size {targetWidth}x{targetHeight} -framerate {targetFps} -q:v 2 -loglevel error -"
                : $"-f v4l2 -input_format mjpeg -video_size {targetWidth}x{targetHeight} -framerate {targetFps} -i {device} -f mpjpeg -boundary_tag ffserver -q:v 2 -loglevel error -";

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
                await activeStreamProcess.StandardOutput.BaseStream.CopyToAsync(context.Response.Body, context.RequestAborted);
            }
            catch (OperationCanceledException) { }
        });

        app.MapPost("/api/camera/stream/stop", () =>
        {
            if (activeStreamProcess != null && !activeStreamProcess.HasExited)
            {
                activeStreamProcess.Kill();
                activeStreamProcess.Dispose();
                activeStreamProcess = null;
            }
            return Results.Json(new { success = true });
        });

        // --- Settings API (POST) ---
        app.MapPost("/api/camera/set", (CameraSetting setting, ICameraController camera) =>
        {
            if (!Enum.TryParse<CameraProperty>(setting.Prop, true, out var prop))
            {
                return Results.Json(new { success = false, message = "Invalid property" });
            }

            bool success = camera.SetProperty(prop, setting.Val);
            return Results.Json(new { success });
        });

        app.MapPost("/api/camera/reset", (ICameraController camera) =>
        {
            camera.ResetToDefaults();
            return Results.Json(new { success = true });
        });

        app.MapPost("/api/camera/save", (ICameraController camera) =>
        {
            return Results.Json(new { success = true, message = "Settings updated in hardware registers." });
        });

        app.MapGet("/api/camera/controls", (ICameraController camera) => {
            return Results.Json(new { success = true, raw = camera.GetControls() });
        });

        app.MapGet("/api/camera/formats", (ICameraController camera) => {
            return Results.Json(new { success = true, formats = camera.GetSupportedFormats() });
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
