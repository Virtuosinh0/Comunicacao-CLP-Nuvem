namespace DataManager.Service.Config;

/// <summary>Envio para a API em nuvem (seção "Api" do appsettings.json).</summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>Ex.: "http://localhost:8080". Vazio = não envia, só registra o JSON no log.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Como enviar as OPs de cada CLP.</summary>
    public ReactorSendMode ReactorMode { get; set; } = ReactorSendMode.Batch;

    public int TimeoutSeconds { get; set; } = 10;
}

public enum ReactorSendMode
{
    /// <summary>1 POST por CLP com todas as OPs (/api/v1/telemetry/reactors/batch).</summary>
    Batch,

    /// <summary>1 POST por OP (/api/v1/telemetry/reactors).</summary>
    Single
}
