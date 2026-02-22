namespace ElgatoControl.Avalonia.ViewModels;

public class ControlPairViewModel
{
    public ControlViewModel Left { get; }
    public ControlViewModel Right { get; }

    public ControlPairViewModel(ControlViewModel left, ControlViewModel right)
    {
        Left = left;
        Right = right;
    }
}
