using System.Buffers.Binary;
using DataManager.Service.Models;

namespace DataManager.Service.Plc;

/// <summary>
/// Layout do UDT da OP em DB com "Optimized block access" desligado.
/// As OPs ficam em um array contíguo (Array[0..N-1] of UDT) a partir de "OpStartAddress".
/// <code>
/// Offset  Campo           Tipo        Bytes
///   0     codigoOp        String[14]  16
///  16     status          Int          2
///  18     prioridade      Int          2
///  20     quantidadeSP    Real         4
///  24     quantidadePV    Real         4
///  28     nomeProduto     String[12]  14
///  42     dataStart       DTL         12
///  54     dataEnd         DTL         12
///  66     destinoFinal    String[10]  12
///  78     tarefaAtualInt  Int          2
///  80     (tamanho total de 1 OP)
/// </code>
/// Se o UDT mudar no CLP, atualizar os offsets aqui.
/// </summary>
public static class OpLayout
{
    public const int CodigoOp = 0;
    public const int Status = 16;
    public const int Prioridade = 18;
    public const int QuantidadeSp = 20;
    public const int QuantidadePv = 24;
    public const int NomeProduto = 28;
    public const int DataStart = 42;
    public const int DataEnd = 54;
    public const int DestinoFinal = 66;
    public const int TarefaAtual = 90;

    /// <summary>Bytes ocupados por uma OP no array.</summary>
    public const int Size = 160;

    private const int CodigoOpSize = 14 + 2;
    private const int NomeProdutoSize = 12 + 2;
    private const int DestinoFinalSize = 10 + 2;
    private const int DtlSize = 12;

    /// <summary>Valor inicial de um DTL no TIA Portal (DTL#1970-01-01-00:00:00) = "sem data".</summary>
    private static readonly DateTime DtlDefault = new(1970, 1, 1, 0, 0, 0);

    /// <summary>
    /// Decodifica uma OP. Se o codigoOp estiver vazio o tanque está sem OP:
    /// retorna null sem decodificar os outros campos.
    /// </summary>
    public static OpData? Decode(ReadOnlySpan<byte> op, bool clockIsUtc)
    {
        if (op.Length < Size)
            throw new ArgumentException($"OP precisa de {Size} bytes, recebido {op.Length}.", nameof(op));

        var codigoOp = PlcValueDecoder.ReadString(op.Slice(CodigoOp, CodigoOpSize)).Trim();
        if (codigoOp.Length == 0)
            return null;

        return new OpData(
            CodigoOp: codigoOp,
            Status: BinaryPrimitives.ReadInt16BigEndian(op[Status..]),
            Prioridade: BinaryPrimitives.ReadInt16BigEndian(op[Prioridade..]),
            QuantidadeSp: BinaryPrimitives.ReadSingleBigEndian(op[QuantidadeSp..]),
            QuantidadePv: BinaryPrimitives.ReadSingleBigEndian(op[QuantidadePv..]),
            NomeProduto: PlcValueDecoder.ReadString(op.Slice(NomeProduto, NomeProdutoSize)),
            DataStart: ReadOptionalDtl(op.Slice(DataStart, DtlSize), clockIsUtc),
            DataEnd: ReadOptionalDtl(op.Slice(DataEnd, DtlSize), clockIsUtc),
            DestinoFinal: PlcValueDecoder.ReadString(op.Slice(DestinoFinal, DestinoFinalSize)),
            TarefaAtual: BinaryPrimitives.ReadInt16BigEndian(op[TarefaAtual..]));
    }

    /// <summary>DTL zerado, inválido ou no valor inicial (1970-01-01) vira null.</summary>
    private static DateTime? ReadOptionalDtl(ReadOnlySpan<byte> data, bool clockIsUtc)
    {
        var raw = PlcValueDecoder.ReadDtlRaw(data);
        if (raw is null || raw.Value == DtlDefault)
            return null;

        return PlcValueDecoder.ToUtc(raw.Value, clockIsUtc);
    }
}
