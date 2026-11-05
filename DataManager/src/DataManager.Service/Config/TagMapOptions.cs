using DataManager.Service.Models;

namespace DataManager.Service.Config;

/// <summary>
/// Mapa de tags (seção "TagMap" de Config/plc-tags.json).
/// As OPs/reatores não ficam aqui: são fixas no <see cref="PlcCatalog"/>.
/// </summary>
public sealed class TagMapOptions
{
    public const string SectionName = "TagMap";

    public List<MillMap> Mills { get; set; } = new();
}

/// <summary>Um moinho: N tags do CLP → 1 JSON de /api/v1/telemetry/mills.</summary>
public sealed class MillMap
{
    /// <summary>Nome do CLP no <see cref="PlcCatalog"/> ("CLP1", "CLP2").</summary>
    public string Plc { get; set; } = string.Empty;

    public string MillId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PlcTagDefinition> Tags { get; set; } = new();
}
