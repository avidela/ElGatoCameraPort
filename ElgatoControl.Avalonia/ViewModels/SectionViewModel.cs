using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ElgatoControl.Avalonia.ViewModels;

public partial class SectionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _id;

    // Can hold ControlViewModel or ControlPairViewModel
    [ObservableProperty]
    private ObservableCollection<object> _items = new();

    [ObservableProperty]
    private bool _isCollapsed;

    public bool IsFrameSection => Id == "frame";

    public SectionViewModel(string title, string id)
    {
        Title = title;
        Id = id;
    }

    [RelayCommand]
    private void ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
    }
}
