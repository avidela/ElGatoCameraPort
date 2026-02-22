using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ElgatoControl.Avalonia.ViewModels;
using ElgatoControl.Avalonia.Views;
using ElgatoControl.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.Versioning;

namespace ElgatoControl.Avalonia;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Setup DI
        var services = new ServiceCollection();

        // Platform-specific Camera Service
        if (OperatingSystem.IsWindows())
        {
            RegisterWindowsServices(services);
        }
        else if (OperatingSystem.IsLinux())
        {
            RegisterLinuxServices(services);
        }
        else
        {
             // Fallback or error?
             // For now, let's just not register a camera device or mock it if needed.
             // But the app expects one.
        }

        services.AddSingleton<IStreamService, StreamService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    [SupportedOSPlatform("windows")]
    private void RegisterWindowsServices(IServiceCollection services)
    {
        services.AddSingleton<ICameraDevice, WindowsCameraDevice>();
    }

    [SupportedOSPlatform("linux")]
    private void RegisterLinuxServices(IServiceCollection services)
    {
        services.AddSingleton<ICameraDevice, LinuxCameraDevice>();
    }
}
