using DataManager.Service.Models.Api;

namespace DataManager.Service.Http;

/// <summary>Destino dos payloads gerados (um envio por reator/OP e um por moinho).</summary>
public interface ITelemetrySender
{
    /// <summary>POST /api/v1/telemetry/reactors</summary>
    Task SendReactorAsync(ReactorTelemetry payload, CancellationToken cancellationToken);

    /// <summary>POST /api/v1/telemetry/mills</summary>
    Task SendMillAsync(MillTelemetry payload, CancellationToken cancellationToken);
}
