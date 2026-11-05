namespace DataManager.Service.Models;

/// <summary>
/// "JSON CLP": definição de uma variável do CLP, como escrita em Config/plc-tags.json.
/// <code>{ "db": 100, "format": "REAL", "address": "24", "name": "requested_weight_kg" }</code>
/// </summary>
public sealed class PlcTagDefinition
{
    /// <summary>Número do DB (ex.: 100 para DB100).</summary>
    public int Db { get; set; }

    /// <summary>Formato S7: BOOL, BYTE, WORD, DWORD, INT, DINT, REAL, LREAL, STRING, DTL.</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Offset em bytes dentro do DB ("24"). Para BOOL: "byte.bit" ("2.3").</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Nome do campo no JSON da API (ex.: "produced_weight_kg").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Somente STRING: tamanho máximo declarado no CLP (String[n]). Padrão 254.</summary>
    public int? Length { get; set; }
}
