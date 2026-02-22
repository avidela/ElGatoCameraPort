using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace ElgatoControl.Api.Endpoints;

public static class ScreenshotEndpoints
{
    public static void MapScreenshotEndpoints(this WebApplication app)
    {
        // Opens the user's Pictures folder (or Downloads as fallback) in the system file manager
        app.MapGet("/api/screenshots/open-folder", () =>
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));

            if (!Directory.Exists(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{folder}\"",
                    UseShellExecute = false
                });
                return Results.Ok(new { success = true, folder });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { success = false, error = ex.Message });
            }
        });
    }
}
