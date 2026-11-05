using System.Text.Json;
using DataManager.Service.Models.Api;

namespace DataManager.Service.Http;

/// <summary>
/// Implementação provisória: apenas registra no log o JSON que seria enviado para a API.
/// Quando a API estiver pronta, criar um HttpTelemetrySender e trocar o registro no Program.cs.
/// </summary>
public sealed class LoggingTelemetrySender : ITelemetrySender
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly ILogger<LoggingTelemetrySender> _logger;

    public LoggingTelemetrySender(ILogger<LoggingTelemetrySender> logger)
    {
        _logger = logger;
    }

    public Task SendReactorAsync(ReactorTelemetry payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/telemetry/reactors {Json}", JsonSerializer.Serialize(payload, JsonOptions));
        return Task.CompletedTask;
    }

    public Task SendMillAsync(MillTelemetry payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/telemetry/mills {Json}", JsonSerializer.Serialize(payload, JsonOptions));
        return Task.CompletedTask;
    }
}
