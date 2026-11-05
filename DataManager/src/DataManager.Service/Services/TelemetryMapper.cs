using System.Globalization;
using DataManager.Service.Models;
using DataManager.Service.Models.Api;

namespace DataManager.Service.Services;

/// <summary>Converte os dados lidos do CLP nos payloads da API.</summary>
public static class TelemetryMapper
{
    /// <summary>Tags que cada moinho precisa ter no plc-tags.json.</summary>
    public static readonly IReadOnlyList<string> MillTagNames =
    [
        "production_today_kg"
    ];

    /// <summary>
    /// UDT da OP → payload do reator. <paramref name="op"/> null = tanque sem OP (só reactor_id e op_code "").
    /// <paramref name="plcId"/> e <paramref name="timestampUtc"/> null = vão no topo do lote.
    /// </summary>
    public static ReactorTelemetry ToReactor(string reactorId, OpData? op, string? plcId, DateTime? timestampUtc)
    {
        if (op is null)
            return new ReactorTelemetry { PlcId = plcId, ReactorId = reactorId, OpCode = string.Empty, Timestamp = timestampUtc };

        return new ReactorTelemetry
        {
            PlcId = plcId,
            ReactorId = reactorId,
            OpCode = op.CodigoOp.Trim(),
            ProductCode = op.NomeProduto.Trim(),
            Status = op.Status,
            Priority = op.Prioridade,
            RequestedWeightKg = ToDouble(op.QuantidadeSp),
            ProducedWeightKg = ToDouble(op.QuantidadePv),
            CurrentStep = op.TarefaAtual,
            StartTime = op.DataStart,
            EndTime = op.DataEnd,
            FinalDestination = op.DestinoFinal.Trim(),
            Timestamp = timestampUtc
        };
    }

    public static MillTelemetry ToMill(TagGroup group, IReadOnlyDictionary<string, object?> values, DateTime timestampUtc)
    {
        return new MillTelemetry
        {
            MillId = group.Id,
            Name = group.DisplayName ?? group.Id,
            ProductionTodayKg = GetDouble(values, "production_today_kg"),
            Timestamp = timestampUtc
        };
    }

    /// <summary>
    /// REAL do CLP → double. NaN/Infinito (REAL inválido no CLP) vira null: nunca enviar 0 como se fosse leitura real.
    /// Passa por decimal para evitar ruído de precisão do float (ex.: 0.1f → 0.100000001490116).
    /// </summary>
    private static double? ToDouble(float value)
    {
        if (!float.IsFinite(value))
            return null;

        return Math.Abs(value) < 7.9e28f ? (double)(decimal)value : value;
    }

    private static double GetDouble(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Tag '{name}' não foi lida do CLP.");

        var result = value switch
        {
            float f => ToDouble(f),
            null => null,
            var other => Convert.ToDouble(other, CultureInfo.InvariantCulture)
        };

        // Moinho com valor inválido não é enviado (erro no log) em vez de mandar 0.
        return result is { } d && double.IsFinite(d)
            ? d
            : throw new InvalidDataException($"Tag '{name}' com valor inválido no CLP ({value}).");
    }
}
