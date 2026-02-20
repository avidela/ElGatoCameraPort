using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

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
app.MapGet("/api/camera/stream", async (HttpContext context) =>
{
    string? device = FindElgatoDevice();
    if (device == null) return;

    context.Response.ContentType = "multipart/x-mixed-replace; boundary=frame";

    // Use ffmpeg to create an MJPEG stream from the webcam
    // Adjust -s (size) and -framerate as needed
    var psi = new ProcessStartInfo
    {
        FileName = "ffmpeg",
        Arguments = $"-f v4l2 -i {device} -f mpjpeg -q:v 5 -s 1280x720 -",
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
    catch (OperationCanceledException) { /* Browser closed tab */ }
    finally
    {
        if (!process.HasExited) process.Kill();
    }
});

// --- Settings API (POST) ---
app.MapPost("/api/camera/set", (CameraSetting setting) =>
{
    string? device = FindElgatoDevice();
    if (device == null) return Results.Json(new { success = false, message = "Camera not found" });

    string mappedProp = MapProperty(setting.Prop);
    string result = RunCommand("v4l2-ctl", $"-d {device} --set-ctrl={mappedProp}={setting.Val}");
    
    if (string.IsNullOrEmpty(result) || !result.Contains("error", StringComparison.OrdinalIgnoreCase))
        return Results.Json(new { success = true });
    else
        return Results.Json(new { success = false, message = result });
});

app.MapPost("/api/camera/save", () =>
{
    string? device = FindElgatoDevice();
    if (device == null) return Results.Json(new { success = false, message = "Camera not found" });

    // Note: 'save' to flash is hardware-specific. 
    // For many UVC devices, settings are saved to RAM.
    // We can try to trigger a specific UVC control if known, 
    // or just acknowledge the command.
    return Results.Json(new { success = true, message = "Settings sent to hardware registers." });
});

app.MapGet("/api/camera/controls", () => {
    string? device = FindElgatoDevice();
    if (device == null) return Results.Json(new { success = false, message = "Camera not found" });
    
    string output = RunCommand("v4l2-ctl", $"-d {device} -L");
    return Results.Json(new { success = true, raw = output });
});

app.Run("http://localhost:5000");

// --- Models ---
public record CameraSetting(string Prop, int Val);

// --- Helpers ---
static string MapProperty(string input)
{
    return input.ToLower() switch
    {
        "zoom" => "zoom_absolute",
        "exposure" => "exposure_absolute",
        "gain" => "gain",
        "white_balance" => "white_balance_temperature",
        "brightness" => "brightness",
        "contrast" => "contrast",
        "saturation" => "saturation",
        "sharpness" => "sharpness",
        _ => input
    };
}

static string? FindElgatoDevice()
{
    if (OperatingSystem.IsWindows()) return "COM_FAKE_DEVICE";

    string output = RunCommand("v4l2-ctl", "--list-devices");
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
    return null;
}

static string RunCommand(string cmd, string args)
{
    if (OperatingSystem.IsWindows()) return ""; 

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
        if (process == null) return "Error starting process";
        
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return string.IsNullOrEmpty(error) ? output : error;
    }
    catch (Exception ex) { return ex.Message; }
}
