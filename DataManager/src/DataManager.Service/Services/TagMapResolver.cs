using DataManager.Service.Config;
using DataManager.Service.Models;

namespace DataManager.Service.Services;

/// <summary>
/// Valida o Config/plc-tags.json na inicialização. Qualquer erro (formato, endereço,
/// tag obrigatória faltando, nome duplicado) impede o serviço de subir, com mensagem clara.
/// </summary>
public static class TagMapResolver
{
    public static ResolvedTagMap Resolve(TagMapOptions options)
    {
        var errors = new List<string>();

        var reactors = options.Reactors
            .Select((r, i) => ResolveGroup(
                $"Reactors[{i}] ({r.ReactorId})", r.ReactorId, null, r.Tags,
                TelemetryMapper.ReactorTagNames, errors))
            .ToList();

        var mills = options.Mills
            .Select((m, i) => ResolveGroup(
                $"Mills[{i}] ({m.MillId})", m.MillId, m.Name, m.Tags,
                TelemetryMapper.MillTagNames, errors))
            .ToList();

        CheckDuplicateIds("Reactors", reactors, errors);
        CheckDuplicateIds("Mills", mills, errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Config/plc-tags.json inválido:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => " - " + e)));
        }

        return new ResolvedTagMap(reactors, mills);
    }

    private static TagGroup ResolveGroup(
        string label, string id, string? displayName, List<PlcTagDefinition> definitions,
        IReadOnlyList<string> requiredNames, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
            errors.Add($"{label}: id não informado.");

        var tags = new List<PlcTag>();
        foreach (var definition in definitions)
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

        var missing = requiredNames.Where(n =>
            definitions.All(d => !string.Equals(d.Name?.Trim(), n, StringComparison.OrdinalIgnoreCase)));
        foreach (var name in missing)
            errors.Add($"{label}: falta a tag obrigatória '{name}'.");

        return new TagGroup(id?.Trim() ?? string.Empty, displayName?.Trim(), tags);
    }

    private static void CheckDuplicateIds(string label, IEnumerable<TagGroup> groups, List<string> errors)
    {
        foreach (var duplicate in groups.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            errors.Add($"{label}: id '{duplicate.Key}' repetido.");
    }
}
