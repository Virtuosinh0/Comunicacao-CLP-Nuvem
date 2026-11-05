using System.Text.Json.Serialization;

namespace DataManager.Service.Models.Api;

/// <summary>
/// Uma OP/tanque. Usado sozinho em POST /api/v1/telemetry/reactors
/// ou dentro de <see cref="ReactorBatchTelemetry"/> em POST /api/v1/telemetry/reactors/batch.
/// Tanque sem OP: só <c>reactor_id</c> e <c>op_code = ""</c> (campos null não são serializados).
/// </summary>
public sealed record ReactorTelemetry
{
    /// <summary>Só no envio individual; no lote vai uma vez no topo.</summary>
    [JsonPropertyName("plc_id")] public string? PlcId { get; init; }

    [JsonPropertyName("reactor_id")] public required string ReactorId { get; init; }
    [JsonPropertyName("op_code")] public required string OpCode { get; init; }
    [JsonPropertyName("product_code")] public string? ProductCode { get; init; }
    [JsonPropertyName("status")] public int? Status { get; init; }
    [JsonPropertyName("priority")] public int? Priority { get; init; }
    [JsonPropertyName("requested_weight_kg")] public double? RequestedWeightKg { get; init; }
    [JsonPropertyName("produced_weight_kg")] public double? ProducedWeightKg { get; init; }
    [JsonPropertyName("current_step")] public int? CurrentStep { get; init; }
    [JsonPropertyName("start_time")] public DateTime? StartTime { get; init; }
    [JsonPropertyName("end_time")] public DateTime? EndTime { get; init; }
    [JsonPropertyName("final_destination")] public string? FinalDestination { get; init; }

    /// <summary>Só no envio individual; no lote vai uma vez no topo.</summary>
    [JsonPropertyName("timestamp")] public DateTime? Timestamp { get; init; }
}

/// <summary>Payload de POST /api/v1/telemetry/reactors/batch: todas as OPs de um CLP em um envio.</summary>
public sealed record ReactorBatchTelemetry
{
    [JsonPropertyName("plc_id")] public required string PlcId { get; init; }
    [JsonPropertyName("timestamp")] public required DateTime Timestamp { get; init; }
    [JsonPropertyName("reactors")] public required IReadOnlyList<ReactorTelemetry> Reactors { get; init; }
}
