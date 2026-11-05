using DataManager.Service.Models;

namespace DataManager.Service.Config;

/// <summary>
/// CLPs e OPs da planta (fixos no código).
/// A ordem dos nomes é a ordem do array de OPs no DB: 1º nome = OP[0], 2º = OP[1], ...
/// A conexão (IP, rack, slot, DB) de cada CLP fica no appsettings.json, seção "Plc:Connections".
/// </summary>
public static class PlcCatalog
{
    public static readonly IReadOnlyList<PlcDefinition> Plcs =
    [
        new("CLP1",
        [
            "R300", "R301", "R302", "R311", "R312", "SB300", "R230", "R280"
        ]),
        new("CLP2",
        [
            "R210", "R211", "R212", "R280",
            "B380A", "B380B", "B380C",
            "R350", "R351", "R352", "R353", "R354", "R355",
            "R360", "R361", "R362", "R363", "R364",
            "R370", "R371", "R482",
            "AMZ1", "AMZ2", "AMZ3", "AMZ4"
        ])
    ];
}
