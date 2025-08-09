using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed class CameraPollingService : ICameraPollingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private CancellationTokenSource? _cts;
    public event EventHandler<ImageUpdatedEventArgs>? ImageUpdated;

    public CameraPollingService(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Stop();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 1) Load config from server (reads your existing appsettings.json/CameraConfig)
        var api = _httpClientFactory.CreateClient("api");
        var cfg = await api.GetFromJsonAsync<CameraConfigDto>("/api/cameras", _cts.Token)
                 ?? new CameraConfigDto();

        // 2) Start a polling loop per camera (up to 16)
        var count = Math.Min(16, cfg.Cameras?.Count ?? 0);
        for (int i = 0; i < count; i++)
        {
            var cam = cfg.Cameras![i];
            _ = Task.Run(() => PollLoop(cam, cfg, _cts.Token));
        }
    }

    private async Task PollLoop(CameraInfoDto cam, CameraConfigDto cfg, CancellationToken token)
    {
        var interval = cam.IntervalMs is > 0 ? cam.IntervalMs!.Value : Math.Max(500, cfg.DefaultIntervalMs);
        var http = _httpClientFactory.CreateClient("camera");

        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(interval));
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var resp = await http.GetAsync(cam.Url, token);
                    resp.EnsureSuccessStatusCode();

                    var bytes = await resp.Content.ReadAsByteArrayAsync(token);
                    var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";

                    ImageUpdated?.Invoke(this, new ImageUpdatedEventArgs
                    {
                        CameraName = cam.Name,
                        DataUrl = dataUrl
                    });
                }
                catch (OperationCanceledException) { break; }
                catch { /* ignore this tick; try again next tick */ }

                await timer.WaitForNextTickAsync(token);
            }
        }
        finally
        {
            timer.Dispose();
        }
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => Stop();
}

// DTOs that match your appsettings.json structure.
// Unknown fields are ignored by System.Text.Json, so your file can have extra props safely.
public sealed class CameraConfigDto
{
    public int DefaultIntervalMs { get; set; } = 2000;
    public List<CameraInfoDto> Cameras { get; set; } = new();
}

public sealed class CameraInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int? IntervalMs { get; set; }
}