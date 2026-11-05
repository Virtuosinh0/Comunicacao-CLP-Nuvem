using System.Text.Json.Serialization;

namespace DataManager.Service.Models.Api;

/// <summary>Payload de POST /api/v1/telemetry/reactors (uma OP do CLP).</summary>
public sealed record ReactorTelemetry
{
    [JsonPropertyName("reactor_id")] public required string ReactorId { get; init; }
    [JsonPropertyName("op_code")] public required string OpCode { get; init; }
    [JsonPropertyName("product_code")] public required string ProductCode { get; init; }
    [JsonPropertyName("status")] public required int Status { get; init; }
    [JsonPropertyName("priority")] public required int Priority { get; init; }
    [JsonPropertyName("requested_weight_kg")] public required double RequestedWeightKg { get; init; }
    [JsonPropertyName("produced_weight_kg")] public required double ProducedWeightKg { get; init; }
    [JsonPropertyName("current_step")] public required int CurrentStep { get; init; }
    [JsonPropertyName("start_time")] public required DateTime? StartTime { get; init; }
    [JsonPropertyName("end_time")] public required DateTime? EndTime { get; init; }
    [JsonPropertyName("final_destination")] public required string FinalDestination { get; init; }
    [JsonPropertyName("timestamp")] public required DateTime Timestamp { get; init; }
}
