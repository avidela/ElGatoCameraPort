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

app.Run("http://localhost:5000");
