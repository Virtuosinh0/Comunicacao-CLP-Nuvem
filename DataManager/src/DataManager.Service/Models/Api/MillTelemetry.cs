using System.Text.Json.Serialization;

namespace DataManager.Service.Models.Api;

/// <summary>Payload de POST /api/v1/telemetry/mills (conforme README).</summary>
public sealed record MillTelemetry
{
    [JsonPropertyName("mill_id")] public required string MillId { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("production_today_kg")] public required double ProductionTodayKg { get; init; }
    [JsonPropertyName("timestamp")] public required DateTime Timestamp { get; init; }
}
