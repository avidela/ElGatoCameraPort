using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ElgatoControl.Api.Services;
using ElgatoControl.Api.Utilities;

namespace ElgatoControl.Api.Endpoints;

public static class StreamEndpoints
{
    private static Process? activeStreamProcess = null;

    public static void MapStreamEndpoints(this IEndpointRouteBuilder app)
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

            string ffmpegPath = FFmpegUtility.FindFFmpeg();
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
    }
}
