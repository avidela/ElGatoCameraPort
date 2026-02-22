using System;
using System.IO;

namespace ElgatoControl.Core.Utilities;

public static class FFmpegUtility
{
    public static string FindFFmpeg()
    {
        if (OperatingSystem.IsWindows())
        {
            string wingetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                @"Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.0.1-full_build\bin\ffmpeg.exe");
            
            if (File.Exists(wingetPath)) return wingetPath;
            return "ffmpeg.exe";
        }
        return "ffmpeg";
    }
}
