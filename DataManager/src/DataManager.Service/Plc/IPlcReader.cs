using DataManager.Service.Models;

namespace DataManager.Service.Plc;

public interface IPlcReader : IDisposable
{
    bool IsConnected { get; }

    /// <summary>Conecta no CLP (não faz nada se já estiver conectado).</summary>
    void Connect();

    void Disconnect();

    /// <summary>Lê as tags informadas e devolve os valores indexados pelo <see cref="PlcTag.Name"/>.</summary>
    IReadOnlyDictionary<string, object> Read(IReadOnlyList<PlcTag> tags);
}
