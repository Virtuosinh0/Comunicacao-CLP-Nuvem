using DataManager.Service.Config;
using DataManager.Service.Models;
using Sharp7;

namespace DataManager.Service.Plc;

/// <summary>
/// Leitura de UM CLP Siemens via Sharp7 (port C# do Snap7, protocolo S7).
/// SOMENTE LEITURA: o S7Client é privado e a única operação feita no CLP é DBRead.
/// Nunca adicionar escrita (DBWrite/WriteArea/...) nem comandos (Stop/Start/SetDateTime/...).
/// </summary>
public sealed class Sharp7PlcReader : IPlcReader
{
    private readonly S7Client _client = new();
    private readonly string _name;
    private readonly PlcConnectionOptions _connection;
    private readonly int _connectTimeoutMs;
    private readonly bool _clockIsUtc;
    private readonly ILogger _logger;

    public Sharp7PlcReader(string name, PlcConnectionOptions connection, int connectTimeoutMs, bool clockIsUtc, ILogger logger)
    {
        _name = name;
        _connection = connection;
        _connectTimeoutMs = connectTimeoutMs;
        _clockIsUtc = clockIsUtc;
        _logger = logger;
    }

    public bool IsConnected => _client.Connected;

    public void Connect()
    {
        if (_client.Connected)
            return;

        _client.ConnTimeout = _connectTimeoutMs;
        var result = _client.ConnectTo(_connection.Ip, _connection.Rack, _connection.Slot);
        if (result != 0)
        {
            throw new PlcCommunicationException(
                $"{_name}: falha ao conectar em {_connection.Ip} (rack {_connection.Rack}, slot {_connection.Slot}): {_client.ErrorText(result)}",
                result);
        }

        _logger.LogInformation("{Plc}: conectado em {Ip} (rack {Rack}, slot {Slot})",
            _name, _connection.Ip, _connection.Rack, _connection.Slot);
    }

    public void Disconnect()
    {
        if (_client.Connected)
        {
            _client.Disconnect();
            _logger.LogInformation("{Plc}: desconectado de {Ip}", _name, _connection.Ip);
        }
    }

    public byte[] ReadBytes(int db, int start, int size)
    {
        // O Sharp7 divide leituras maiores que o PDU em várias requisições automaticamente.
        var buffer = new byte[size];
        var result = _client.DBRead(db, start, size, buffer);
        if (result != 0)
        {
            throw new PlcCommunicationException(
                $"{_name}: falha ao ler DB{db} (bytes {start} a {start + size - 1}): {_client.ErrorText(result)}",
                result);
        }

        return buffer;
    }

    public IReadOnlyDictionary<string, object?> Read(IReadOnlyList<PlcTag> tags)
    {
        var values = new Dictionary<string, object?>(tags.Count, StringComparer.OrdinalIgnoreCase);

        // Uma única leitura por DB, cobrindo do menor ao maior endereço usado no grupo.
        foreach (var dbTags in tags.GroupBy(t => t.Db))
        {
            var start = dbTags.Min(t => t.ByteOffset);
            var end = dbTags.Max(t => t.ByteOffset + t.Size);
            var buffer = ReadBytes(dbTags.Key, start, end - start);

            foreach (var tag in dbTags)
            {
                var slice = buffer.AsSpan(tag.ByteOffset - start, tag.Size);
                values[tag.Name] = PlcValueDecoder.Decode(tag, slice, _clockIsUtc);
            }
        }

        return values;
    }

    public void Dispose() => Disconnect();
}
