using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace ApiHost;

/// <summary>
/// Roda "java -jar clp-nuvem-api.jar" e mantém a API no ar:
///  - se a API cair, reinicia após RestartDelaySeconds;
///  - ao parar o serviço, encerra o Java (e filhos);
///  - a saída da API vai para logs\api-AAAA-MM-DD.log;
///  - se existir a pasta "static" ao lado do jar, a API serve a tela (FrontEnd compilado) nela.
/// </summary>
public sealed class JavaProcessService(IOptions<JavaOptions> options, ILogger<JavaProcessService> logger) : BackgroundService
{
    private readonly JavaOptions _options = options.Value;
    private readonly string _baseDir = AppContext.BaseDirectory.TrimEnd('\\');
    private readonly object _logLock = new();
    private readonly KillOnCloseJob _job = new();

    public override void Dispose()
    {
        _job.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var logDir = Path.Combine(_baseDir, "logs");
        Directory.CreateDirectory(logDir);

        while (!stoppingToken.IsCancellationRequested)
        {
            DeleteOldLogs(logDir);
            var startInfo = BuildStartInfo();

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => WriteLog(logDir, e.Data);
            process.ErrorDataReceived += (_, e) => WriteLog(logDir, e.Data);

            try
            {
                process.Start();
                _job.Add(process);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Não foi possível iniciar a API: {Exe} {Args}", startInfo.FileName, startInfo.Arguments);
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } // não chegou a iniciar
                await DelayRestart(stoppingToken);
                continue;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            logger.LogInformation("API iniciada (PID {Pid}): {Exe} {Args}", process.Id, startInfo.FileName, startInfo.Arguments);

            try
            {
                await process.WaitForExitAsync(stoppingToken);
                logger.LogWarning("API parou sozinha (código {Code}). Reiniciando em {Delay} s. Veja {LogDir}.",
                    process.ExitCode, _options.RestartDelaySeconds, logDir);
                await DelayRestart(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Serviço parando: encerra o Java.
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(15000);
                }
                logger.LogInformation("API encerrada.");
            }
        }
    }

    private ProcessStartInfo BuildStartInfo()
    {
        var jar = Path.Combine(_baseDir, _options.Jar);
        var args = $"-jar \"{jar}\"";

        var staticDir = Path.Combine(_baseDir, "static");
        if (Directory.Exists(staticDir))
            args += $" --spring.web.resources.static-locations=file:{staticDir.Replace('\\', '/')}/";

        if (!string.IsNullOrWhiteSpace(_options.Arguments))
            args += " " + _options.Arguments;

        return new ProcessStartInfo(ResolveJava(), args)
        {
            WorkingDirectory = _baseDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
    }

    private string ResolveJava()
    {
        if (!string.IsNullOrWhiteSpace(_options.JavaExe))
            return _options.JavaExe;

        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var exe = Path.Combine(javaHome, "bin", "java.exe");
            if (File.Exists(exe))
                return exe;
        }

        return "java";
    }

    private Task DelayRestart(CancellationToken token) =>
        Task.Delay(TimeSpan.FromSeconds(_options.RestartDelaySeconds), token)
            .ContinueWith(_ => { }, TaskScheduler.Default);

    private void WriteLog(string logDir, string? line)
    {
        if (line is null)
            return;

        var file = Path.Combine(logDir, $"api-{DateTime.Now:yyyy-MM-dd}.log");
        lock (_logLock)
        {
            try
            {
                File.AppendAllText(file, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // Log é auxiliar: não derruba a API por falha de disco.
            }
        }
    }

    private void DeleteOldLogs(string logDir)
    {
        var limit = DateTime.Now.AddDays(-_options.LogRetentionDays);
        foreach (var file in Directory.EnumerateFiles(logDir, "api-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < limit)
                    File.Delete(file);
            }
            catch (IOException)
            {
            }
        }
    }
}
