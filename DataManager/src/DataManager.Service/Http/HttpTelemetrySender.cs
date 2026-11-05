using System.Net.Http.Json;
using DataManager.Service.Config;
using DataManager.Service.Models.Api;
using Microsoft.Extensions.Options;

namespace DataManager.Service.Http;

/// <summary>Envia os payloads para a API em nuvem (Api:BaseUrl).</summary>
public sealed class HttpTelemetrySender : ITelemetrySender, IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpTelemetrySender> _logger;

    public HttpTelemetrySender(IOptions<ApiOptions> options, ILogger<HttpTelemetrySender> logger)
    {
        var api = options.Value;
        _http = new HttpClient
        {
            BaseAddress = new Uri(api.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(Math.Max(1, api.TimeoutSeconds))
        };
        _logger = logger;
    }

    public Task SendReactorAsync(ReactorTelemetry payload, CancellationToken cancellationToken) =>
        PostAsync(TelemetryRoutes.Reactor, payload, cancellationToken);

    public Task SendReactorBatchAsync(ReactorBatchTelemetry payload, CancellationToken cancellationToken) =>
        PostAsync(TelemetryRoutes.ReactorBatch, payload, cancellationToken);

    public Task SendMillAsync(MillTelemetry payload, CancellationToken cancellationToken) =>
        PostAsync(TelemetryRoutes.Mill, payload, cancellationToken);

    private async Task PostAsync<T>(string route, T payload, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync(route, payload, LoggingTelemetrySender.JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"POST /{route} retornou {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
                null, response.StatusCode);
        }

        _logger.LogDebug("POST /{Route} → {Status}", route, (int)response.StatusCode);
    }

    public void Dispose() => _http.Dispose();
}
