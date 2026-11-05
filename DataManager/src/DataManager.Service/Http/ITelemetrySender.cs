using DataManager.Service.Models.Api;

namespace DataManager.Service.Http;

/// <summary>Destino dos payloads gerados.</summary>
public interface ITelemetrySender
{
    /// <summary>POST /api/v1/telemetry/reactors (uma OP)</summary>
    Task SendReactorAsync(ReactorTelemetry payload, CancellationToken cancellationToken);

    /// <summary>POST /api/v1/telemetry/reactors/batch (todas as OPs de um CLP)</summary>
    Task SendReactorBatchAsync(ReactorBatchTelemetry payload, CancellationToken cancellationToken);

    /// <summary>POST /api/v1/telemetry/mills</summary>
    Task SendMillAsync(MillTelemetry payload, CancellationToken cancellationToken);
}

internal static class TelemetryRoutes
{
    public const string Reactor = "api/v1/telemetry/reactors";
    public const string ReactorBatch = "api/v1/telemetry/reactors/batch";
    public const string Mill = "api/v1/telemetry/mills";
}
