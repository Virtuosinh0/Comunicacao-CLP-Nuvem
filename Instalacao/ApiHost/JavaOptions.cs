namespace ApiHost;

public sealed class JavaOptions
{
    public const string SectionName = "Java";

    /// <summary>Caminho do java.exe. Vazio = JAVA_HOME\bin\java.exe, senão "java" do PATH.</summary>
    public string JavaExe { get; set; } = "";

    /// <summary>Jar da API, relativo à pasta do serviço.</summary>
    public string Jar { get; set; } = "clp-nuvem-api.jar";

    /// <summary>Argumentos extras para a API (ex.: --server.port=8080).</summary>
    public string Arguments { get; set; } = "";

    /// <summary>Espera antes de reiniciar a API se ela cair.</summary>
    public int RestartDelaySeconds { get; set; } = 10;

    /// <summary>Dias de log da API (pasta logs) mantidos.</summary>
    public int LogRetentionDays { get; set; } = 14;
}
