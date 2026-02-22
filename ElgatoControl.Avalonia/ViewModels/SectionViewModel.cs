using CommunityToolkit.Mvvm.ComponentModel;
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

    public SectionViewModel(string title, string id)
    {
        Title = title;
        Id = id;
    }
}
