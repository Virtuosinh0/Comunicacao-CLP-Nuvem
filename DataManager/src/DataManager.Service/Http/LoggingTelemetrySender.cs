using System.Text.Json;
using System.Text.Json.Serialization;
using DataManager.Service.Models.Api;

namespace DataManager.Service.Http;

/// <summary>Usado quando Api:BaseUrl está vazio: apenas registra no log o JSON que seria enviado.</summary>
public sealed class LoggingTelemetrySender : ITelemetrySender
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<LoggingTelemetrySender> _logger;

    public LoggingTelemetrySender(ILogger<LoggingTelemetrySender> logger)
    {
        _logger = logger;
    }

    public Task SendReactorAsync(ReactorTelemetry payload, CancellationToken cancellationToken) =>
        Log(TelemetryRoutes.Reactor, payload);

    public Task SendReactorBatchAsync(ReactorBatchTelemetry payload, CancellationToken cancellationToken) =>
        Log(TelemetryRoutes.ReactorBatch, payload);

    public Task SendMillAsync(MillTelemetry payload, CancellationToken cancellationToken) =>
        Log(TelemetryRoutes.Mill, payload);

    private Task Log<T>(string route, T payload)
    {
        _logger.LogInformation("POST /{Route} {Json}", route, JsonSerializer.Serialize(payload, JsonOptions));
        return Task.CompletedTask;
    }
}
