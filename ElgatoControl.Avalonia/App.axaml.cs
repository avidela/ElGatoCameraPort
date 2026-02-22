using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ElgatoControl.Avalonia.ViewModels;
using ElgatoControl.Avalonia.Views;
using ElgatoControl.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

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
            services.AddSingleton<ICameraDevice, WindowsCameraDevice>();
        }
        else
        {
            services.AddSingleton<ICameraDevice, LinuxCameraDevice>();
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
}
