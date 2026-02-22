using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ElgatoControl.Api.Services;
using ElgatoControl.Api.Endpoints;

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

// Register all abstracted camera endpoints
app.MapCameraEndpoints();

app.Run("http://localhost:5000");
