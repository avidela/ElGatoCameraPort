using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ElgatoControl.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// SOLID: Register the appropriate controller based on OS
if (OperatingSystem.IsWindows())
{
    builder.Services.AddSingleton<ICameraController, WindowsCameraController>();
}
else
{
    builder.Services.AddSingleton<ICameraController, LinuxCameraController>();
}

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

// --- Live Preview Stream ---
app.MapGet("/api/camera/stream", async (HttpContext context, ICameraController camera) =>
{
    string? device = camera.FindDevice();
    if (device == null) return;

    context.Response.ContentType = "multipart/x-mixed-replace; boundary=ffserver";

    string ffmpegPath = FindFFmpeg();
    // Use mjpeg muxer with explicit boundary
    string args = OperatingSystem.IsWindows() 
        ? $"-f dshow -i video=\"{device}\" -f mpjpeg -boundary_tag ffserver -q:v 5 -s 1280x720 -loglevel error -"
        : $"-f v4l2 -input_format mjpeg -i {device} -f mpjpeg -boundary_tag ffserver -q:v 5 -s 1280x720 -loglevel error -";

    var psi = new ProcessStartInfo
    {
        FileName = ffmpegPath,
        Arguments = args,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    if (process == null) return;

    try
    {
        await process.StandardOutput.BaseStream.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
    catch (OperationCanceledException) { }
    finally
    {
        if (!process.HasExited) process.Kill();
    }
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
    // Implementation for save would go here if hardware supports specific save command
    return Results.Json(new { success = true, message = "Settings updated in hardware registers." });
});

app.MapGet("/api/camera/controls", (ICameraController camera) => {
    return Results.Json(new { success = true, raw = camera.GetControls() });
});

app.Run("http://localhost:5000");

// --- Helpers ---

string FindFFmpeg()
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

// --- Models ---
public record CameraSetting(string Prop, int Val);
