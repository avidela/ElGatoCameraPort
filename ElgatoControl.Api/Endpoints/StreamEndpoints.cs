using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ElgatoControl.Core.Services;

namespace ElgatoControl.Api.Endpoints;

public static class StreamEndpoints
{
    public static void MapStreamEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Live Preview Stream ---
        app.MapGet("/api/camera/stream", async (HttpContext context, ICameraDevice camera, IStreamService streamService, int? w, int? h, int? fps) =>
        {
            string? device = camera.FindDevice();
            if (device == null) return;

            // Use requested format or default to 1080p60
            int targetWidth = w ?? 1920;
            int targetHeight = h ?? 1080;
            int targetFps = fps ?? 60;

            context.Response.ContentType = "multipart/x-mixed-replace; boundary=ffserver";

            var (stream, streamId) = await streamService.StartStreamAsync(device, targetWidth, targetHeight, targetFps, context.RequestAborted);
            if (stream == null) return;

            try
            {
                // Custom manual chunk loop to instantly flush frames to the browser instead of letting ASP.NET Core buffer them
                byte[] buffer = new byte[81920]; 
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, context.RequestAborted)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer, 0, bytesRead, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { /* Handle stream disconnects gracefully */ }
            finally
            {
                // Ensure stream is stopped when client disconnects, but only if it's the stream we started
                await streamService.StopStreamAsync(streamId);
            }
        });

        app.MapPost("/api/camera/stream/stop", async (IStreamService streamService) =>
        {
            await streamService.StopActiveStreamAsync();
            return Results.Json(new { success = true });
        });
    }
}
