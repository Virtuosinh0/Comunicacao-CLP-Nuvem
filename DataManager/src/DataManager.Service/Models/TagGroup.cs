namespace DataManager.Service.Models;

/// <summary>
/// Conjunto de N tags do CLP que, juntas, formam UM JSON da API
/// (ex.: todas as variáveis do reator R-300).
/// </summary>
public sealed record TagGroup(string Id, string? DisplayName, IReadOnlyList<PlcTag> Tags);

/// <summary>Mapa completo de tags, já validado.</summary>
public sealed record ResolvedTagMap(IReadOnlyList<TagGroup> Reactors, IReadOnlyList<TagGroup> Mills);
