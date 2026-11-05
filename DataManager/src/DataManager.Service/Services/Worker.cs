using System.Diagnostics;
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
    private readonly ApiOptions _api;
    private readonly IReadOnlyList<(PlcStation Station, IPlcReader Plc)> _stations;

    public Worker(
        ILogger<Worker> logger,
        ILoggerFactory loggerFactory,
        IReadOnlyList<PlcStation> stations,
        ITelemetrySender sender,
        IOptions<PlcOptions> options,
        IOptions<ApiOptions> api)
    {
        _logger = logger;
        _sender = sender;
        _options = options.Value;
        _api = api.Value;

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

        _logger.LogInformation("DataManager iniciado: ciclo de {Interval}s, envio de OPs: {Mode}, destino: {Url}",
            _options.PollIntervalSeconds, _api.ReactorMode,
            string.IsNullOrWhiteSpace(_api.BaseUrl) ? "(somente log)" : _api.BaseUrl);

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                var started = Stopwatch.GetTimestamp();
                foreach (var (station, plc) in _stations)
                    await RunStationCycleAsync(station, plc, stoppingToken);

                var elapsed = Stopwatch.GetElapsedTime(started);
                if (elapsed > interval)
                {
                    _logger.LogWarning("Ciclo levou {Ms:0} ms, maior que o intervalo de {Interval}s: ciclos serão pulados",
                        elapsed.TotalMilliseconds, _options.PollIntervalSeconds);
                }
                else if (_stations.Count > 1)
                {
                    _logger.LogInformation("Ciclo completo ({Plcs} CLPs) em {Ms:0} ms", _stations.Count, elapsed.TotalMilliseconds);
                }
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

    private async Task RunStationCycleAsync(PlcStation station, IPlcReader plc, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            plc.Connect();
        }
        catch (PlcCommunicationException ex)
        {
            _logger.LogWarning("{Message} ({Ms:0} ms)", ex.Message, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return;
        }

        try
        {
            await ProcessOpsAsync(station, plc, cancellationToken);

            foreach (var mill in station.Mills)
                await ProcessMillAsync(mill, plc, cancellationToken);
        }
        catch (PlcCommunicationException ex)
        {
            // Perda de comunicação: desconecta e tenta reconectar no próximo ciclo.
            _logger.LogWarning("{Message}", ex.Message);
            plc.Disconnect();
        }
    }

    /// <summary>
    /// Lê o array de OPs inteiro em uma leitura. Tanque com codigoOp vazio não tem os outros campos decodificados.
    /// Envia conforme Api:ReactorMode: 1 POST com todas (Batch) ou 1 POST por OP (Single).
    /// </summary>
    private async Task ProcessOpsAsync(PlcStation station, IPlcReader plc, CancellationToken cancellationToken)
    {
        var ops = station.Definition.Ops;
        var connection = station.Connection;
        var batch = _api.ReactorMode == ReactorSendMode.Batch;

        var t0 = Stopwatch.GetTimestamp();
        var buffer = plc.ReadBytes(connection.OpDb, connection.OpStartAddress, ops.Count * OpLayout.Size);
        var t1 = Stopwatch.GetTimestamp();
        var timestamp = TruncateToSeconds(DateTime.UtcNow); // momento real da leitura deste CLP

        var payloads = new List<ReactorTelemetry>(ops.Count);
        var active = 0;
        for (var i = 0; i < ops.Count; i++)
        {
            try
            {
                var op = OpLayout.Decode(buffer.AsSpan(i * OpLayout.Size, OpLayout.Size), _options.ClockIsUtc);
                if (op is not null)
                    active++;

                payloads.Add(batch
                    ? TelemetryMapper.ToReactor(ops[i], op, plcId: null, timestampUtc: null)
                    : TelemetryMapper.ToReactor(ops[i], op, station.Name, timestamp));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Plc}: erro ao converter a OP[{Index}] ({Id})", station.Name, i, ops[i]);
            }
        }

        var t2 = Stopwatch.GetTimestamp();

        if (batch)
        {
            var payload = new ReactorBatchTelemetry { PlcId = station.Name, Timestamp = timestamp, Reactors = payloads };
            await SendAsync(station.Name, () => _sender.SendReactorBatchAsync(payload, cancellationToken));
        }
        else
        {
            foreach (var payload in payloads)
                await SendAsync(payload.ReactorId, () => _sender.SendReactorAsync(payload, cancellationToken));
        }

        var t3 = Stopwatch.GetTimestamp();

        _logger.LogInformation(
            "{Plc}: {Total:0} ms (leitura {Read:0} ms, conversão {Decode:0.0} ms, envio {Send:0} ms em {Requests} POST) - {Active}/{Ops} OP(s) ativas",
            station.Name,
            Stopwatch.GetElapsedTime(t0, t3).TotalMilliseconds,
            Stopwatch.GetElapsedTime(t0, t1).TotalMilliseconds,
            Stopwatch.GetElapsedTime(t1, t2).TotalMilliseconds,
            Stopwatch.GetElapsedTime(t2, t3).TotalMilliseconds,
            batch ? 1 : payloads.Count,
            active, ops.Count);
    }

    private async Task ProcessMillAsync(TagGroup mill, IPlcReader plc, CancellationToken cancellationToken)
    {
        MillTelemetry payload;
        try
        {
            var values = plc.Read(mill.Tags);
            payload = TelemetryMapper.ToMill(mill, values, TruncateToSeconds(DateTime.UtcNow));
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
            _logger.LogError("Erro ao enviar os dados de {Id}: {Message}", id, ex.Message);
        }
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
}
