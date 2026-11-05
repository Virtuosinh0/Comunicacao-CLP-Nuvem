namespace DataManager.Service.Models;

/// <summary>Uma OP como lida do CLP (UDT da OP, ver <see cref="Plc.OpLayout"/>).</summary>
public sealed record OpData(
    string CodigoOp,
    short Status,
    float QuantidadeSp,
    float QuantidadePv,
    string NomeProduto,
    DateTime? DataStart,
    DateTime? DataEnd,
    short TarefaAtual);
