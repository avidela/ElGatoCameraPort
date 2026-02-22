using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ElgatoControl.Core.Utilities;

namespace ElgatoControl.Core.Services;

public class StreamService : IStreamService, IAsyncDisposable
{
    private Process? _activeStreamProcess;
    private Guid _activeStreamId;
    private readonly object _lock = new object();

    public Task<(Stream? Stream, Guid StreamId)> StartStreamAsync(string device, int width, int height, int fps, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            StopStreamInternal();

            _activeStreamId = Guid.NewGuid();

            string ffmpegPath = FFmpegUtility.FindFFmpeg();
            // Use -c:v copy to pass the MJPEG stream directly without re-encoding, preserving pixel-perfect quality and reducing latency
            string args = OperatingSystem.IsWindows()
                ? $"-f dshow -vcodec mjpeg -video_size {width}x{height} -framerate {fps} -i video=\"{device}\" -fflags nobuffer -c:v copy -f mpjpeg -boundary_tag ffserver -loglevel error -"
                : $"-f v4l2 -input_format mjpeg -video_size {width}x{height} -framerate {fps} -i {device} -fflags nobuffer -c:v copy -f mpjpeg -boundary_tag ffserver -loglevel error -";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _activeStreamProcess = Process.Start(psi);
            if (_activeStreamProcess == null) return Task.FromResult<(Stream? Stream, Guid StreamId)>((null, Guid.Empty));

            return Task.FromResult<(Stream? Stream, Guid StreamId)>((_activeStreamProcess.StandardOutput.BaseStream, _activeStreamId));
        }
    }

    public Task StopStreamAsync(Guid streamId)
    {
        lock (_lock)
        {
            if (_activeStreamId == streamId)
            {
                StopStreamInternal();
            }
        }
        return Task.CompletedTask;
    }

    public Task StopActiveStreamAsync()
    {
        lock (_lock)
        {
            StopStreamInternal();
        }
        return Task.CompletedTask;
    }

    private void StopStreamInternal()
    {
        if (_activeStreamProcess != null)
        {
            try
            {
                if (!_activeStreamProcess.HasExited)
                {
                    _activeStreamProcess.Kill();
                }
                _activeStreamProcess.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping stream process: {ex.Message}");
            }
            finally
            {
                _activeStreamProcess = null;
                _activeStreamId = Guid.Empty;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            StopStreamInternal();
        }
        return ValueTask.CompletedTask;
    }
}
