using System.Net;
using DataManager.Service.Config;
using DataManager.Service.Models;

namespace DataManager.Service.Services;

/// <summary>
/// Valida, na inicialização, o <see cref="PlcCatalog"/>, a conexão de cada CLP (appsettings.json)
/// e o Config/plc-tags.json. Qualquer erro impede o serviço de subir, com mensagem clara.
/// </summary>
public static class TagMapResolver
{
    public static IReadOnlyList<PlcStation> Resolve(
        IReadOnlyList<PlcDefinition> catalog, PlcOptions plcOptions, TagMapOptions tagMap)
    {
        var errors = new List<string>();

        foreach (var duplicate in catalog.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"PlcCatalog: CLP '{duplicate.Key}' repetido.");

        foreach (var plc in catalog)
        {
            if (plc.Ops.Count == 0)
                errors.Add($"PlcCatalog: {plc.Name} sem OPs.");

            foreach (var duplicate in plc.Ops.GroupBy(o => o, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                errors.Add($"PlcCatalog: OP '{duplicate.Key}' repetida no {plc.Name}.");
        }

        var connections = new Dictionary<string, PlcConnectionOptions>(plcOptions.Connections, StringComparer.OrdinalIgnoreCase);
        foreach (var unknown in connections.Keys.Where(k => catalog.All(p => !string.Equals(p.Name, k, StringComparison.OrdinalIgnoreCase))))
            errors.Add($"appsettings.json: Plc:Connections:{unknown} não existe no PlcCatalog.");

        var mills = tagMap.Mills
            .Select((m, i) => (m.Plc, Group: ResolveMill($"Mills[{i}] ({m.MillId})", m, errors)))
            .ToList();

        foreach (var duplicate in mills.GroupBy(m => m.Group.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"plc-tags.json: moinho '{duplicate.Key}' repetido.");

        foreach (var (plc, group) in mills.Where(m => catalog.All(p => !string.Equals(p.Name, m.Plc?.Trim(), StringComparison.OrdinalIgnoreCase))))
            errors.Add($"plc-tags.json: moinho '{group.Id}' com plc '{plc}' inexistente. Use: {string.Join(", ", catalog.Select(p => p.Name))}.");

        var stations = new List<PlcStation>();
        foreach (var plc in catalog)
        {
            if (!connections.TryGetValue(plc.Name, out var connection))
            {
                errors.Add($"appsettings.json: falta a conexão Plc:Connections:{plc.Name}.");
                continue;
            }

            if (!connection.Enabled)
                continue;

            ValidateConnection(plc, connection, errors);

            var plcMills = mills
                .Where(m => string.Equals(m.Plc?.Trim(), plc.Name, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Group)
                .ToList();

            stations.Add(new PlcStation(plc, connection, plcMills));
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuração inválida:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => " - " + e)));
        }

        return stations;
    }

    private static void ValidateConnection(PlcDefinition plc, PlcConnectionOptions c, List<string> errors)
    {
        var label = $"appsettings.json: Plc:Connections:{plc.Name}";

        if (!string.IsNullOrWhiteSpace(c.IpEnvironmentVariable))
        {
            var name = c.IpEnvironmentVariable.Trim();
            var ip = ReadEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(ip))
            {
                errors.Add($"{label}: variável de ambiente '{name}' (IP do CLP) não definida.");
                return;
            }

            c.Ip = ip.Trim();
        }

        if (!IPAddress.TryParse(c.Ip, out _))
            errors.Add($"{label}: Ip '{c.Ip}' inválido.");

        if (c.OpDb <= 0)
            errors.Add($"{label}: OpDb deve ser o número do DB das OPs (> 0).");

        if (c.OpStartAddress < 0 || c.OpStartAddress % 2 != 0)
            errors.Add($"{label}: OpStartAddress deve ser par e >= 0 (o array de UDT começa em endereço par).");
    }

    /// <summary>
    /// Processo → usuário → máquina. Lê usuário/máquina direto do registro para funcionar mesmo
    /// se a variável foi criada depois que o VS Code / o Serviço Windows foi iniciado.
    /// </summary>
    private static string? ReadEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
        ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);

    private static TagGroup ResolveMill(string label, MillMap mill, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(mill.MillId))
            errors.Add($"{label}: millId não informado.");

        var tags = new List<PlcTag>();
        foreach (var definition in mill.Tags)
        {
            try
            {
                tags.Add(PlcTag.From(definition));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"{label}: {ex.Message}");
            }
        }

        foreach (var duplicate in tags.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"{label}: tag '{duplicate.Key}' declarada mais de uma vez.");

        var missing = TelemetryMapper.MillTagNames.Where(n =>
            mill.Tags.All(d => !string.Equals(d.Name?.Trim(), n, StringComparison.OrdinalIgnoreCase)));
        foreach (var name in missing)
            errors.Add($"{label}: falta a tag obrigatória '{name}'.");

        return new TagGroup(mill.MillId?.Trim() ?? string.Empty, mill.Name?.Trim(), tags);
    }
}
