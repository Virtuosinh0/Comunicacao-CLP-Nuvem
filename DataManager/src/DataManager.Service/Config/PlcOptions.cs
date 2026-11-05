namespace DataManager.Service.Config;

/// <summary>Comunicação com os CLPs (seção "Plc" do appsettings.json).</summary>
public sealed class PlcOptions
{
    public const string SectionName = "Plc";

    public int ConnectTimeoutMs { get; set; } = 2000;
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>true se o relógio dos CLPs estiver em UTC; false se estiver no horário local do gateway.</summary>
    public bool ClockIsUtc { get; set; }

    /// <summary>Conexão de cada CLP do <see cref="PlcCatalog"/>, pelo nome ("CLP1", "CLP2").</summary>
    public Dictionary<string, PlcConnectionOptions> Connections { get; set; } = new();
}

/// <summary>Conexão de um CLP e localização do array de OPs.</summary>
public sealed class PlcConnectionOptions
{
    /// <summary>false = CLP ignorado (não conecta nem lê).</summary>
    public bool Enabled { get; set; } = true;

    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Nome de uma variável de ambiente com o IP (ex.: "CLP_Producao"). Se informada, tem prioridade sobre <see cref="Ip"/>.
    /// Procurada no processo, depois no usuário, depois na máquina.
    /// </summary>
    public string? IpEnvironmentVariable { get; set; }
    public int Rack { get; set; } = 0;

    /// <summary>S7-1200/1500: normalmente slot 1 (ou 0). S7-300: slot 2.</summary>
    public int Slot { get; set; } = 1;

    /// <summary>Número do DB que contém o array de OPs.</summary>
    public int OpDb { get; set; }

    /// <summary>Offset (bytes) do início do array de OPs dentro do DB. Normalmente 0.</summary>
    public int OpStartAddress { get; set; } = 0;
}
