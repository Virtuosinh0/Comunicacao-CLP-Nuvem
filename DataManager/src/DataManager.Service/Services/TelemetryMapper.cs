using System.Globalization;
using DataManager.Service.Models;
using DataManager.Service.Models.Api;

namespace DataManager.Service.Services;

/// <summary>
/// Converte os valores lidos de N tags do CLP ("JSON CLP") em UM payload da API ("JSON API").
/// O "name" de cada tag no plc-tags.json deve ser igual ao nome do campo no JSON da API.
/// </summary>
public static class TelemetryMapper
{
    /// <summary>Tags que cada reator precisa ter no plc-tags.json.</summary>
    public static readonly IReadOnlyList<string> ReactorTagNames =
    [
        "op_code",
        "product_code",
        "requested_weight_kg",
        "produced_weight_kg",
        "current_step",
        "start_time"
    ];

    /// <summary>Tags que cada moinho precisa ter no plc-tags.json.</summary>
    public static readonly IReadOnlyList<string> MillTagNames =
    [
        "production_today_kg"
    ];

    public static ReactorTelemetry ToReactor(TagGroup group, IReadOnlyDictionary<string, object> values, DateTime timestampUtc)
    {
        return new ReactorTelemetry
        {
            ReactorId = group.Id,
            OpCode = GetString(values, "op_code"),
            ProductCode = GetString(values, "product_code"),
            RequestedWeightKg = GetDouble(values, "requested_weight_kg"),
            ProducedWeightKg = GetDouble(values, "produced_weight_kg"),
            CurrentStep = GetInt(values, "current_step"),
            StartTime = GetDateTime(values, "start_time"),
            Timestamp = timestampUtc
        };
    }

    public static MillTelemetry ToMill(TagGroup group, IReadOnlyDictionary<string, object> values, DateTime timestampUtc)
    {
        return new MillTelemetry
        {
            MillId = group.Id,
            Name = group.DisplayName ?? group.Id,
            ProductionTodayKg = GetDouble(values, "production_today_kg"),
            Timestamp = timestampUtc
        };
    }

    private static object Get(IReadOnlyDictionary<string, object> values, string name)
    {
        return values.TryGetValue(name, out var value)
            ? value
            : throw new KeyNotFoundException($"Tag '{name}' não foi lida do CLP.");
    }

    private static string GetString(IReadOnlyDictionary<string, object> values, string name)
    {
        return Get(values, name) switch
        {
            string s => s.Trim(),
            var other => Convert.ToString(other, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static double GetDouble(IReadOnlyDictionary<string, object> values, string name)
    {
        return Get(values, name) switch
        {
            // Passa por decimal para evitar ruído de precisão do float (ex.: 0.1f → 0.100000001490116).
            float f => (double)(decimal)f,
            var other => Convert.ToDouble(other, CultureInfo.InvariantCulture)
        };
    }

    private static int GetInt(IReadOnlyDictionary<string, object> values, string name)
    {
        return Convert.ToInt32(Get(values, name), CultureInfo.InvariantCulture);
    }

    private static DateTime GetDateTime(IReadOnlyDictionary<string, object> values, string name)
    {
        return Get(values, name) is DateTime dateTime
            ? dateTime
            : throw new InvalidCastException($"Tag '{name}' deveria ser do formato DTL.");
    }
}
