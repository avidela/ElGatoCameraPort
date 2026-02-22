using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElgatoControl.Core.Services;

public interface IStreamService
{
    Task<(Stream? Stream, Guid StreamId)> StartStreamAsync(string device, int width, int height, int fps, CancellationToken cancellationToken = default);
    Task StopStreamAsync(Guid streamId);
    Task StopActiveStreamAsync();
}
