namespace DataManager.Service.Config;

/// <summary>Conexão com o CLP (seção "Plc" do appsettings.json).</summary>
public sealed class PlcOptions
{
    public const string SectionName = "Plc";

    public string Ip { get; set; } = "192.168.0.1";
    public int Rack { get; set; } = 0;

    /// <summary>S7-1200/1500: normalmente slot 1 (ou 0). S7-300: slot 2.</summary>
    public int Slot { get; set; } = 1;

    public int ConnectTimeoutMs { get; set; } = 2000;
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>true se o relógio do CLP estiver em UTC; false se estiver no horário local do gateway.</summary>
    public bool ClockIsUtc { get; set; }
}
