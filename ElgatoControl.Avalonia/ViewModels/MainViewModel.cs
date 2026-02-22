using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElgatoControl.Core.Services;
using ElgatoControl.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using Avalonia.Threading;

namespace ElgatoControl.Avalonia.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ICameraDevice _cameraDevice;
    private readonly IStreamService _streamService;
    private CancellationTokenSource? _previewCts;
    private Guid _currentStreamId;

    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    private ObservableCollection<SectionViewModel> _sections = new();

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private bool _isPreviewActive;

    [ObservableProperty]
    private string _previewStatus = "Preview Off";

    [ObservableProperty]
    private bool _isDeviceCollapsed;

    [RelayCommand]
    private void ToggleDeviceCollapse() => IsDeviceCollapsed = !IsDeviceCollapsed;

    [ObservableProperty]
    private bool _showGrid = true;

    [RelayCommand]
    private void ToggleGrid() => ShowGrid = !ShowGrid;

    [ObservableProperty]
    private VideoFormat? _selectedFormat;

    [ObservableProperty]
    private ObservableCollection<VideoFormat> _supportedFormats = new();

    [ObservableProperty]
    private ControlViewModel? _panVm;

    [ObservableProperty]
    private ControlViewModel? _tiltVm;

    [ObservableProperty]
    private ControlViewModel? _zoomVm;

    public MainViewModel(ICameraDevice cameraDevice, IStreamService streamService)
    {
        _cameraDevice = cameraDevice;
        _streamService = streamService;
        InitializeFormats();
        LoadLayout();
    }

    private void InitializeFormats()
    {
        var formats = _cameraDevice.GetSupportedFormats().ToList();
        foreach (var f in formats) SupportedFormats.Add(f);
        
        // Default to 1080p60 if available, else first
        SelectedFormat = formats.FirstOrDefault(f => f.Width == 1920 && f.Height == 1080 && f.Fps == 60) 
                         ?? formats.FirstOrDefault();
    }

    public MainViewModel()
    {
        _cameraDevice = null!;
        _streamService = null!;
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

        // Cache important ViewModels for ZoomPreview
        if (control.Id == "pan") PanVm = controlVm;
        if (control.Id == "tilt") TiltVm = controlVm;
        if (control.Id == "zoom") ZoomVm = controlVm;

        return controlVm;
    }

    [RelayCommand]
    private void Preset(string id)
    {
        // Preset values (standardized percentages)
        var presets = new Dictionary<string, (int p, int t, int z)>
        {
            { "A", (0, 0, 100) },
            { "B", (-50, 20, 200) },
            { "C", (50, -20, 300) },
            { "D", (0, 0, 400) }
        };

        if (presets.TryGetValue(id, out var p))
        {
            PanVm?.UpdateValue(p.p);
            TiltVm?.UpdateValue(p.t);
            ZoomVm?.UpdateValue(p.z);
            
            // Explicitly set properties on device
            _cameraDevice.SetProperty(CameraProperty.Pan, p.p);
            _cameraDevice.SetProperty(CameraProperty.Tilt, p.t);
            _cameraDevice.SetProperty(CameraProperty.Zoom, p.z);
        }
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        _cameraDevice.ResetToDefaults();
        
        // Refresh all ViewModels from the device
        var currentValues = _cameraDevice.GetControlValues();
        foreach (var section in Sections)
        {
            foreach (var item in section.Items)
            {
                if (item is ControlViewModel cvm)
                {
                    if (currentValues.TryGetValue(cvm.Id, out var val)) cvm.UpdateValue(val);
                }
                else if (item is ControlPairViewModel cpvm)
                {
                    if (currentValues.TryGetValue(cpvm.Left.Id, out var leftVal)) cpvm.Left.UpdateValue(leftVal);
                    if (currentValues.TryGetValue(cpvm.Right.Id, out var rightVal)) cpvm.Right.UpdateValue(rightVal);
                }
            }
        }
    }

    [RelayCommand]
    private async Task TogglePreview()
    {
        if (IsPreviewActive)
        {
            await StopPreviewAsync();
        }
        else
        {
            await StartPreviewAsync();
        }
    }

    partial void OnSelectedFormatChanged(VideoFormat? value)
    {
        if (IsPreviewActive && value != null)
        {
            // Restart preview with new format
            _ = Task.Run(async () =>
            {
                await StopPreviewAsync();
                await StartPreviewAsync();
            });
        }
    }

    private async Task StartPreviewAsync()
    {
        if (IsPreviewActive) return;

        try
        {
            PreviewStatus = "Connecting...";
            IsPreviewActive = true;

            string device = _cameraDevice.FindDevice() ?? "/dev/video0";
            
            int w = SelectedFormat?.Width ?? 1280;
            int h = SelectedFormat?.Height ?? 720;
            int fps = SelectedFormat?.Fps ?? 30;

            var result = await _streamService.StartStreamAsync(device, w, h, fps);

            if (result.Stream == null)
            {
                PreviewStatus = "Failed to start stream";
                IsPreviewActive = false;
                return;
            }

            _currentStreamId = result.StreamId;
            _previewCts = new CancellationTokenSource();

            // Start reading the stream in background
            _ = Task.Run(() => ReadMjpegStream(result.Stream, _previewCts.Token), _previewCts.Token);
            PreviewStatus = "Live";
        }
        catch (Exception ex)
        {
            PreviewStatus = $"Error: {ex.Message}";
            IsPreviewActive = false;
        }
    }

    private async Task StopPreviewAsync()
    {
        if (!IsPreviewActive) return;

        try
        {
            _previewCts?.Cancel();
            if (_currentStreamId != Guid.Empty)
            {
                await _streamService.StopStreamAsync(_currentStreamId);
            }
        }
        finally
        {
            IsPreviewActive = false;
            PreviewStatus = "Preview Off";
            PreviewImage = null;
            _currentStreamId = Guid.Empty;
        }
    }

    [RelayCommand]
    private async Task Snapshot()
    {
        if (PreviewImage == null) return;

        try
        {
            var fileName = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), fileName);

            // Ensure directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Using Task.Run to offload IO
            await Task.Run(() =>
            {
                using var stream = File.Create(path);
                PreviewImage.Save(stream);
            });

            var oldStatus = PreviewStatus;
            PreviewStatus = $"Saved: {fileName}";

            // Revert status message after delay
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                if (PreviewStatus.StartsWith("Saved"))
                {
                   Dispatcher.UIThread.Post(() => PreviewStatus = IsPreviewActive ? "Live" : "Preview Off");
                }
            });
        }
        catch (Exception ex)
        {
            PreviewStatus = $"Snapshot Error: {ex.Message}";
        }
    }

    private async Task ReadMjpegStream(Stream stream, CancellationToken token)
    {
        try
        {
            byte[] buffer = new byte[8192];
            List<byte> frameBuffer = new List<byte>(1024 * 1024); // Preallocate 1MB
            bool inFrame = false;
            bool lastByteWasFF = false;

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break; // End of stream

                for (int i = 0; i < bytesRead; i++)
                {
                    byte b = buffer[i];

                    if (!inFrame)
                    {
                        if (lastByteWasFF)
                        {
                            if (b == 0xD8) // SOI found (FF D8)
                            {
                                inFrame = true;
                                frameBuffer.Clear();
                                frameBuffer.Add(0xFF);
                                frameBuffer.Add(0xD8);
                            }
                            lastByteWasFF = false;
                        }
                        else if (b == 0xFF)
                        {
                            lastByteWasFF = true;
                        }
                    }
                    else
                    {
                        frameBuffer.Add(b);
                        if (lastByteWasFF)
                        {
                            if (b == 0xD9) // EOI found (FF D9)
                            {
                                // Render frame
                                var frameData = frameBuffer.ToArray();
                                await UpdatePreviewImage(frameData);

                                inFrame = false;
                                frameBuffer.Clear();
                            }
                            // Important: update lastByteWasFF for the next iteration (e.g. FF FF case)
                            lastByteWasFF = (b == 0xFF);
                        }
                        else if (b == 0xFF)
                        {
                            lastByteWasFF = true;
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PreviewStatus = $"Stream Error: {ex.Message}";
            });
        }
    }

    private async Task UpdatePreviewImage(byte[] imageData)
    {
        try
        {
            using var ms = new MemoryStream(imageData);
            var bitmap = new Bitmap(ms);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var old = PreviewImage;
                PreviewImage = bitmap;
                old?.Dispose();
            });
        }
        catch
        {
            // Ignore bad frames
        }
    }

    public void Dispose()
    {
        _previewCts?.Cancel();
        if (_currentStreamId != Guid.Empty)
        {
            _streamService.StopStreamAsync(_currentStreamId).Wait();
        }
    }
}
