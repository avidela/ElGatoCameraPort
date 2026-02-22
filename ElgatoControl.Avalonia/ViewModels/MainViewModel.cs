using CommunityToolkit.Mvvm.ComponentModel;
using ElgatoControl.Core.Services;
using ElgatoControl.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace ElgatoControl.Avalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICameraDevice _cameraDevice;

    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    private ObservableCollection<SectionViewModel> _sections = new();

    public MainViewModel(ICameraDevice cameraDevice)
    {
        _cameraDevice = cameraDevice;
        LoadLayout();
    }

    public MainViewModel()
    {
        _cameraDevice = null!;
    }

    private void LoadLayout()
    {
        var layout = _cameraDevice.GetLayout();
        var currentValues = _cameraDevice.GetControlValues();

        foreach (var section in layout)
        {
            var sectionVm = new SectionViewModel(section.Title, section.Id);
            var controls = section.Controls.ToList();

            for (int i = 0; i < controls.Count; i++)
            {
                var c1 = controls[i];

                // Check for Pan/Tilt pair
                if (c1.Id == "pan" && i + 1 < controls.Count)
                {
                    var c2 = controls[i+1];
                    if (c2.Id == "tilt")
                    {
                        var panVm = CreateControlVm(c1, currentValues);
                        var tiltVm = CreateControlVm(c2, currentValues);
                        sectionVm.Items.Add(new ControlPairViewModel(panVm, tiltVm));
                        i++; // Skip tilt
                        continue;
                    }
                }

                sectionVm.Items.Add(CreateControlVm(c1, currentValues));
            }
            Sections.Add(sectionVm);
        }
    }

    private ControlViewModel CreateControlVm(CameraControl control, Dictionary<string, int> currentValues)
    {
        var controlVm = new ControlViewModel(control, _cameraDevice);
        if (currentValues.TryGetValue(control.Id, out var val))
        {
            controlVm.UpdateValue(val);
        }
        return controlVm;
    }
}
