using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using ElgatoControl.Core.Models;

namespace ElgatoControl.Core.Services;

[SupportedOSPlatform("windows")]
public class WindowsCameraDevice : ICameraDevice
{
    private const string TargetDeviceName = "@device_pnp_\\\\?\\usb#vid_0fd9&pid_0093&mi_00#b&17cf1500&0&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\\global";

    public string? FindDevice() => TargetDeviceName;

    public bool SetProperty(CameraProperty property, int value)
    {
        IBaseFilter? device = GetDeviceFilter(TargetDeviceName);
        if (device == null) return false;

        try
        {
            switch (property)
            {
                case CameraProperty.Zoom:
                case CameraProperty.Exposure:
                    return SetCameraControlProperty(device, property, value);
                
                case CameraProperty.Brightness:
                case CameraProperty.Contrast:
                case CameraProperty.Saturation:
                case CameraProperty.Sharpness:
                case CameraProperty.Gain:
                case CameraProperty.WhiteBalance:
                    return SetVideoProcAmpProperty(device, property, value);
                
                default:
                    return false;
            }
        }
        finally
        {
            if (device != null) Marshal.ReleaseComObject(device);
        }
    }

    public string GetControls() => "Windows DirectShow API active";

    public Dictionary<string, int> GetControlValues() => new Dictionary<string, int>();

    public void ResetToDefaults()
    {
        // Placeholder for Windows reset logic
    }

    public IEnumerable<VideoFormat> GetSupportedFormats()
    {
        // Placeholder implementation for Windows
        return new List<VideoFormat> 
        { 
            new VideoFormat("MJPG", 1920, 1080, 60),
            new VideoFormat("MJPG", 1280, 720, 60),
            new VideoFormat("YUYV", 1920, 1080, 30)
        };
    }

    public IEnumerable<ControlSectionData> GetLayout()
    {
        return new List<ControlSectionData>
        {
            new ControlSectionData("Frame", "frame", new List<CameraControl>
            {
                new CameraControl("zoom", "Zoom / FOV", 100, 400, 1, 100, "%"),
                new CameraControl("pan", "Pan", -2592000, 2592000, 3600, 0),
                new CameraControl("tilt", "Tilt", -1458000, 1458000, 3600, 0)
            }),
            new ControlSectionData("Picture", "picture", new List<CameraControl>
            {
                new CameraControl("contrast", "Contrast", 0, 100, 1, 80, "%"),
                new CameraControl("saturation", "Saturation", 0, 127, 1, 64, "%"),
                new CameraControl("sharpness", "Sharpness", 0, 255, 1, 128)
            }),
            new ControlSectionData("Exposure", "exposure", new List<CameraControl>
            {
                new CameraControl("exposure", "Shutter Speed", 1, 2500, 1, 156),
                new CameraControl("gain", "ISO (Gain)", 0, 88, 1, 0),
                new CameraControl("white_balance", "White Balance", 2800, 7500, 10, 5000, "K"),
                new CameraControl("brightness", "Brightness", -9, 9, 1, 0)
            })
        };
    }

    private bool SetCameraControlProperty(IBaseFilter device, CameraProperty property, int value)
    {
        if (device is IAMCameraControl cameraControl)
        {
            CameraControlProperty prop = property switch
            {
                CameraProperty.Zoom => CameraControlProperty.Zoom,
                CameraProperty.Exposure => CameraControlProperty.Exposure,
                _ => throw new ArgumentException("Not a camera control property")
            };

            int hr = cameraControl.Set(prop, value, CameraControlFlags.Manual);
            return hr == 0;
        }
        return false;
    }

    private bool SetVideoProcAmpProperty(IBaseFilter device, CameraProperty property, int value)
    {
        if (device is IAMVideoProcAmp procAmp)
        {
            VideoProcAmpProperty prop = property switch
            {
                CameraProperty.Brightness => VideoProcAmpProperty.Brightness,
                CameraProperty.Contrast => VideoProcAmpProperty.Contrast,
                CameraProperty.Saturation => VideoProcAmpProperty.Saturation,
                CameraProperty.Sharpness => VideoProcAmpProperty.Sharpness,
                CameraProperty.Gain => VideoProcAmpProperty.Gain,
                CameraProperty.WhiteBalance => VideoProcAmpProperty.WhiteBalance,
                _ => throw new ArgumentException("Not a video proc amp property")
            };

            int hr = procAmp.Set(prop, value, VideoProcAmpFlags.Manual);
            return hr == 0;
        }
        return false;
    }

    private IBaseFilter? GetDeviceFilter(string name)
    {
        ICreateDevEnum? devEnum = Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_SystemDeviceEnum)!) as ICreateDevEnum;
        if (devEnum == null) return null;

        IEnumMoniker? enumMoniker;
        Guid category = CLSID_VideoInputDeviceCategory;
        devEnum.CreateClassEnumerator(ref category, out enumMoniker, 0);
        if (enumMoniker == null) return null;

        IMoniker[] monikers = new IMoniker[1];
        IntPtr fetched = IntPtr.Zero;

        while (enumMoniker.Next(1, monikers, fetched) == 0)
        {
            Guid iidPropertyBag = IID_IPropertyBag;
            monikers[0].BindToStorage(null!, null, ref iidPropertyBag, out object bagObj);
            IPropertyBag bag = (IPropertyBag)bagObj;
            bag.Read("FriendlyName", out object val, null!);
            string friendlyName = (string)val;

            if (friendlyName.Contains("Elgato Facecam", StringComparison.OrdinalIgnoreCase))
            {
                Guid iidBaseFilter = IID_IBaseFilter;
                monikers[0].BindToObject(null!, null, ref iidBaseFilter, out object filterObj);
                return (IBaseFilter)filterObj;
            }
            Marshal.ReleaseComObject(monikers[0]);
        }
        return null;
    }

    // --- COM Interfaces & Constants ---

    [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyBag
    {
        void Read([MarshalAs(UnmanagedType.LPWStr)] string propName, out object var, [In] object? errorLog);
        void Write(string propName, ref object var);
    }

    [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICreateDevEnum
    {
        void CreateClassEnumerator([In] ref Guid category, out IEnumMoniker? enumMoniker, [In] int flags);
    }

    [ComImport, Guid("56A86895-3224-11D2-9EEB-006008039E37"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IBaseFilter { }

    [ComImport, Guid("C6E13370-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAMCameraControl
    {
        int GetRange(CameraControlProperty property, out int min, out int max, out int stepping, out int defaultValue, out CameraControlFlags capsFlags);
        int Set(CameraControlProperty property, int lValue, CameraControlFlags flags);
        int Get(CameraControlProperty property, out int lValue, out CameraControlFlags flags);
    }

    [ComImport, Guid("C6E13360-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAMVideoProcAmp
    {
        int GetRange(VideoProcAmpProperty property, out int min, out int max, out int stepping, out int defaultValue, out VideoProcAmpFlags capsFlags);
        int Set(VideoProcAmpProperty property, int lValue, VideoProcAmpFlags flags);
        int Get(VideoProcAmpProperty property, out int lValue, out VideoProcAmpFlags flags);
    }

    private enum CameraControlProperty { Pan, Tilt, Roll, Zoom, Exposure, Iris, Focus }
    private enum CameraControlFlags { Auto = 0x0001, Manual = 0x0002 }
    private enum VideoProcAmpProperty { Brightness, Contrast, Hue, Saturation, Sharpness, Gamma, ColorEnable, WhiteBalance, BacklightCompensation, Gain }
    private enum VideoProcAmpFlags { Auto = 0x0001, Manual = 0x0002 }

    private static readonly Guid CLSID_SystemDeviceEnum = new Guid("62BE5D10-60EB-11d0-BD3B-00A0C911CE86");
    private static readonly Guid CLSID_VideoInputDeviceCategory = new Guid("860BB310-5D01-11D0-BD3B-00A0C911CE86");
    private static readonly Guid IID_IPropertyBag = new Guid("55272A00-42CB-11CE-8135-00AA004BB851");
    private static readonly Guid IID_IBaseFilter = new Guid("56A86895-3224-11D2-9EEB-006008039E37");
}
