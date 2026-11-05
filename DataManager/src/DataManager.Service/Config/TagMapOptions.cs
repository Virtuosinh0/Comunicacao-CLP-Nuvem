using DataManager.Service.Models;

namespace DataManager.Service.Config;

/// <summary>Mapa de tags (seção "TagMap" de Config/plc-tags.json).</summary>
public sealed class TagMapOptions
{
    public const string SectionName = "TagMap";

    public List<ReactorMap> Reactors { get; set; } = new();
    public List<MillMap> Mills { get; set; } = new();
}

/// <summary>Um reator/OP: N tags do CLP → 1 JSON de /api/v1/telemetry/reactors.</summary>
public sealed class ReactorMap
{
    public string ReactorId { get; set; } = string.Empty;
    public List<PlcTagDefinition> Tags { get; set; } = new();
}

/// <summary>Um moinho: N tags do CLP → 1 JSON de /api/v1/telemetry/mills.</summary>
public sealed class MillMap
{
    public string MillId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PlcTagDefinition> Tags { get; set; } = new();
}
