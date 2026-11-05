using DataManager.Service.Config;
using DataManager.Service.Models;
using Microsoft.Extensions.Options;
using Sharp7;

namespace DataManager.Service.Plc;

/// <summary>Leitura do CLP Siemens via Sharp7 (port C# do Snap7, protocolo S7).</summary>
public sealed class Sharp7PlcReader : IPlcReader
{
    private readonly S7Client _client = new();
    private readonly PlcOptions _options;
    private readonly ILogger<Sharp7PlcReader> _logger;

    public Sharp7PlcReader(IOptions<PlcOptions> options, ILogger<Sharp7PlcReader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _client.Connected;

    public void Connect()
    {
        if (_client.Connected)
            return;

        _client.ConnTimeout = _options.ConnectTimeoutMs;
        var result = _client.ConnectTo(_options.Ip, _options.Rack, _options.Slot);
        if (result != 0)
        {
            throw new PlcCommunicationException(
                $"Falha ao conectar no CLP {_options.Ip} (rack {_options.Rack}, slot {_options.Slot}): {_client.ErrorText(result)}",
                result);
        }

        _logger.LogInformation("Conectado ao CLP {Ip} (rack {Rack}, slot {Slot})", _options.Ip, _options.Rack, _options.Slot);
    }

    public void Disconnect()
    {
        if (_client.Connected)
        {
            _client.Disconnect();
            _logger.LogInformation("Desconectado do CLP {Ip}", _options.Ip);
        }
    }

    public IReadOnlyDictionary<string, object> Read(IReadOnlyList<PlcTag> tags)
    {
        var values = new Dictionary<string, object>(tags.Count, StringComparer.OrdinalIgnoreCase);

        // Uma única leitura por DB, cobrindo do menor ao maior endereço usado no grupo.
        foreach (var dbTags in tags.GroupBy(t => t.Db))
        {
            var start = dbTags.Min(t => t.ByteOffset);
            var end = dbTags.Max(t => t.ByteOffset + t.Size);
            var buffer = new byte[end - start];

            var result = _client.DBRead(dbTags.Key, start, buffer.Length, buffer);
            if (result != 0)
            {
                throw new PlcCommunicationException(
                    $"Falha ao ler DB{dbTags.Key} (bytes {start} a {end - 1}): {_client.ErrorText(result)}",
                    result);
            }

            foreach (var tag in dbTags)
            {
                var slice = buffer.AsSpan(tag.ByteOffset - start, tag.Size);
                values[tag.Name] = PlcValueDecoder.Decode(tag, slice, _options.ClockIsUtc);
            }
        }

        return values;
    }

    public void Dispose() => Disconnect();
}
