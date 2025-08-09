using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class ImageUpdatedEventArgs : EventArgs
{
    public required string CameraName { get; init; }
    public required string DataUrl { get; init; }
}

public interface ICameraPollingService : IDisposable
{
    event EventHandler<ImageUpdatedEventArgs>? ImageUpdated;
    Task StartAsync(CancellationToken cancellationToken = default);
    void Stop();
}

