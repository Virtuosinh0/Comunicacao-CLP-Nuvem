using DataManager.Service.Config;
using DataManager.Service.Http;
using DataManager.Service.Models;
using DataManager.Service.Plc;
using Microsoft.Extensions.Options;

namespace DataManager.Service.Services;

/// <summary>
/// Ciclo principal do serviço: a cada intervalo lê os grupos de tags do CLP,
/// converte cada grupo em um payload da API e entrega ao <see cref="ITelemetrySender"/>.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IPlcReader _plc;
    private readonly ResolvedTagMap _tagMap;
    private readonly ITelemetrySender _sender;
    private readonly PlcOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IPlcReader plc,
        ResolvedTagMap tagMap,
        ITelemetrySender sender,
        IOptions<PlcOptions> options)
    {
        _logger = logger;
        _plc = plc;
        _tagMap = tagMap;
        _sender = sender;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DataManager iniciado: {Reactors} reator(es), {Mills} moinho(s), ciclo de {Interval}s",
            _tagMap.Reactors.Count, _tagMap.Mills.Count, _options.PollIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)));
        try
        {
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Serviço parando.
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            _plc.Connect();
        }
        catch (PlcCommunicationException ex)
        {
            _logger.LogWarning("{Message}", ex.Message);
            return;
        }

        var timestamp = TruncateToSeconds(DateTime.UtcNow);

        try
        {
            foreach (var group in _tagMap.Reactors)
            {
                await ProcessGroupAsync(group,
                    values => TelemetryMapper.ToReactor(group, values, timestamp),
                    _sender.SendReactorAsync, cancellationToken);
            }

            foreach (var group in _tagMap.Mills)
            {
                await ProcessGroupAsync(group,
                    values => TelemetryMapper.ToMill(group, values, timestamp),
                    _sender.SendMillAsync, cancellationToken);
            }
        }
        catch (PlcCommunicationException ex)
        {
            // Perda de comunicação: desconecta e tenta reconectar no próximo ciclo.
            _logger.LogWarning("{Message}", ex.Message);
            _plc.Disconnect();
        }
    }

    /// <summary>
    /// N tags do CLP → 1 payload da API → envio.
    /// Erros de conversão/envio afetam só este grupo; erros de comunicação com o CLP sobem.
    /// </summary>
    private async Task ProcessGroupAsync<TPayload>(
        TagGroup group,
        Func<IReadOnlyDictionary<string, object>, TPayload> map,
        Func<TPayload, CancellationToken, Task> send,
        CancellationToken cancellationToken)
    {
        TPayload payload;
        try
        {
            payload = map(_plc.Read(group.Tags));
        }
        catch (Exception ex) when (ex is not PlcCommunicationException)
        {
            _logger.LogError(ex, "Erro ao converter os dados de {Id}", group.Id);
            return;
        }

        try
        {
            await send(payload, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Erro ao enviar os dados de {Id}", group.Id);
        }
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
}
