using DataManager.Service.Config;

namespace DataManager.Service.Models;

/// <summary>
/// Conjunto de N tags do CLP que, juntas, formam UM JSON da API
/// (ex.: todas as variáveis do moinho M-200).
/// </summary>
public sealed record TagGroup(string Id, string? DisplayName, IReadOnlyList<PlcTag> Tags);

/// <summary>Um CLP pronto para leitura: conexão, OPs (do catálogo) e moinhos (do plc-tags.json).</summary>
public sealed record PlcStation(PlcDefinition Definition, PlcConnectionOptions Connection, IReadOnlyList<TagGroup> Mills)
{
    public string Name => Definition.Name;
}
