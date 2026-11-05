using DataManager.Service.Models;

namespace DataManager.Service.Plc;

/// <summary>Conexão com UM CLP.</summary>
public interface IPlcReader : IDisposable
{
    bool IsConnected { get; }

    /// <summary>Conecta no CLP (não faz nada se já estiver conectado).</summary>
    void Connect();

    void Disconnect();

    /// <summary>Lê <paramref name="size"/> bytes do DB a partir de <paramref name="start"/>.</summary>
    byte[] ReadBytes(int db, int start, int size);

    /// <summary>Lê as tags informadas e devolve os valores indexados pelo <see cref="PlcTag.Name"/>.</summary>
    IReadOnlyDictionary<string, object?> Read(IReadOnlyList<PlcTag> tags);
}
