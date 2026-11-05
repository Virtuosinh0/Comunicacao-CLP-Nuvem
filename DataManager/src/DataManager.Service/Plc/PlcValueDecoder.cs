using System.Buffers.Binary;
using System.Text;
using DataManager.Service.Models;

namespace DataManager.Service.Plc;

/// <summary>Converte os bytes lidos do DB (big-endian, padrão S7) em valores .NET.</summary>
public static class PlcValueDecoder
{
    public static object Decode(PlcTag tag, ReadOnlySpan<byte> data, bool clockIsUtc)
    {
        return tag.Type switch
        {
            PlcDataType.Bool => (object)((data[0] & (1 << tag.Bit)) != 0),
            PlcDataType.Byte => data[0],
            PlcDataType.Word => BinaryPrimitives.ReadUInt16BigEndian(data),
            PlcDataType.DWord => BinaryPrimitives.ReadUInt32BigEndian(data),
            PlcDataType.Int => BinaryPrimitives.ReadInt16BigEndian(data),
            PlcDataType.DInt => BinaryPrimitives.ReadInt32BigEndian(data),
            PlcDataType.Real => BinaryPrimitives.ReadSingleBigEndian(data),
            PlcDataType.LReal => BinaryPrimitives.ReadDoubleBigEndian(data),
            PlcDataType.String => DecodeString(data),
            PlcDataType.Dtl => DecodeDtl(data, clockIsUtc),
            _ => throw new NotSupportedException($"Formato {tag.Type} não suportado.")
        };
    }

    /// <summary>STRING S7: [0] tamanho máximo, [1] tamanho atual, [2..] caracteres.</summary>
    private static string DecodeString(ReadOnlySpan<byte> data)
    {
        var maxLength = data[0];
        var currentLength = data[1];
        var length = Math.Min(currentLength, Math.Min(maxLength, data.Length - 2));
        return Encoding.Latin1.GetString(data.Slice(2, length)).TrimEnd('\0', ' ');
    }

    /// <summary>DTL S7: ano(2) mês dia diaSemana hora min seg nanossegundos(4).</summary>
    private static DateTime DecodeDtl(ReadOnlySpan<byte> data, bool clockIsUtc)
    {
        var year = BinaryPrimitives.ReadUInt16BigEndian(data);
        var month = data[2];
        var day = data[3];
        var hour = data[5];
        var minute = data[6];
        var second = data[7];
        var nanoseconds = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(8));

        var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified)
            .AddTicks(nanoseconds / 100);

        return clockIsUtc
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.Local);
    }
}
