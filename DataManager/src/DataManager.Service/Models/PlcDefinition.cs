namespace DataManager.Service.Models;

/// <summary>Um CLP e os nomes das OPs do seu array, na ordem do array no DB.</summary>
public sealed record PlcDefinition(string Name, IReadOnlyList<string> Ops);
