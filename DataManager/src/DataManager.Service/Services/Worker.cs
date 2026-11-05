using DataManager.Service.Config;
using DataManager.Service.Http;
using DataManager.Service.Models;
using DataManager.Service.Models.Api;
using DataManager.Service.Plc;
using Microsoft.Extensions.Options;

namespace DataManager.Service.Services;

/// <summary>
/// Ciclo principal do serviço: a cada intervalo, para cada CLP, lê o array de OPs e os moinhos,
/// converte em payloads da API e entrega ao <see cref="ITelemetrySender"/>.
/// Um CLP fora do ar não impede a leitura dos outros.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITelemetrySender _sender;
    private readonly PlcOptions _options;
    private readonly IReadOnlyList<(PlcStation Station, IPlcReader Plc)> _stations;

    public Worker(
        ILogger<Worker> logger,
        ILoggerFactory loggerFactory,
        IReadOnlyList<PlcStation> stations,
        ITelemetrySender sender,
        IOptions<PlcOptions> options)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;

        var readerLogger = loggerFactory.CreateLogger<Sharp7PlcReader>();
        _stations = stations
            .Select(s => (s, (IPlcReader)new Sharp7PlcReader(
                s.Name, s.Connection, _options.ConnectTimeoutMs, _options.ClockIsUtc, readerLogger)))
            .ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var (station, _) in _stations)
        {
            _logger.LogInformation(
                "{Plc}: {Ip}, {Ops} OP(s) no DB{Db}, {Mills} moinho(s)",
                station.Name, station.Connection.Ip, station.Definition.Ops.Count,
                station.Connection.OpDb, station.Mills.Count);
        }

        _logger.LogInformation("DataManager iniciado: ciclo de {Interval}s", _options.PollIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)));
        try
        {
            do
            {
                var timestamp = TruncateToSeconds(DateTime.UtcNow);
                foreach (var (station, plc) in _stations)
                    await RunStationCycleAsync(station, plc, timestamp, stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Serviço parando.
        }
    }

    public override void Dispose()
    {
        foreach (var (_, plc) in _stations)
            plc.Dispose();

        base.Dispose();
    }

    private async Task RunStationCycleAsync(PlcStation station, IPlcReader plc, DateTime timestamp, CancellationToken cancellationToken)
    {
        try
        {
            plc.Connect();
        }
        catch (PlcCommunicationException ex)
        {
            _logger.LogWarning("{Message}", ex.Message);
            return;
        }

        try
        {
            await ProcessOpsAsync(station, plc, timestamp, cancellationToken);

            foreach (var mill in station.Mills)
                await ProcessMillAsync(mill, plc, timestamp, cancellationToken);
        }
        catch (PlcCommunicationException ex)
        {
            // Perda de comunicação: desconecta e tenta reconectar no próximo ciclo.
            _logger.LogWarning("{Message}", ex.Message);
            plc.Disconnect();
        }
    }

    /// <summary>Lê o array de OPs inteiro em uma leitura e envia um payload por OP.</summary>
    private async Task ProcessOpsAsync(PlcStation station, IPlcReader plc, DateTime timestamp, CancellationToken cancellationToken)
    {
        var ops = station.Definition.Ops;
        var size = ops.Count * OpLayout.Size;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var buffer = plc.ReadBytes(station.Connection.OpDb, station.Connection.OpStartAddress, size);
        _logger.LogDebug("{Plc}: lidas {Ops} OP(s) do DB{Db} (bytes {Start} a {End}) em {Ms:0} ms",
            station.Name, ops.Count, station.Connection.OpDb, station.Connection.OpStartAddress,
            station.Connection.OpStartAddress + size - 1,
            System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);

        for (var i = 0; i < ops.Count; i++)
        {
            ReactorTelemetry payload;
            try
            {
                var op = OpLayout.Decode(buffer.AsSpan(i * OpLayout.Size, OpLayout.Size), _options.ClockIsUtc);
                payload = TelemetryMapper.ToReactor(ops[i], op, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Plc}: erro ao converter a OP[{Index}] ({Id})", station.Name, i, ops[i]);
                continue;
            }

            await SendAsync(ops[i], () => _sender.SendReactorAsync(payload, cancellationToken));
        }
    }

    private async Task ProcessMillAsync(TagGroup mill, IPlcReader plc, DateTime timestamp, CancellationToken cancellationToken)
    {
        MillTelemetry payload;
        try
        {
            payload = TelemetryMapper.ToMill(mill, plc.Read(mill.Tags), timestamp);
        }
        catch (Exception ex) when (ex is not PlcCommunicationException)
        {
            _logger.LogError(ex, "Erro ao converter os dados de {Id}", mill.Id);
            return;
        }

        await SendAsync(mill.Id, () => _sender.SendMillAsync(payload, cancellationToken));
    }

    /// <summary>Erro de envio afeta só este payload.</summary>
    private async Task SendAsync(string id, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Erro ao enviar os dados de {Id}", id);
        }
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
}
