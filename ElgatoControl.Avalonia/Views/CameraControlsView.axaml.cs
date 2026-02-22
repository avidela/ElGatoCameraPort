using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ElgatoControl.Avalonia.ViewModels;

namespace ElgatoControl.Avalonia.Views;

public partial class CameraControlsView : UserControl
{
    public CameraControlsView()
    {
        InitializeComponent();
    }

    private void OnToggleDeviceCollapse(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsDeviceCollapsed = !vm.IsDeviceCollapsed;
        }
    }

    private void OnToggleCollapse(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is SectionViewModel svm)
        {
            svm.IsCollapsed = !svm.IsCollapsed;
        }
    }
}
