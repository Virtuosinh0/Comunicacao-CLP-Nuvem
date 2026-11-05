using System.Buffers.Binary;
using System.Text;
using DataManager.Service.Models;

namespace DataManager.Service.Plc;

/// <summary>Converte os bytes lidos do DB (big-endian, padrão S7) em valores .NET.</summary>
public static class PlcValueDecoder
{
    public static object? Decode(PlcTag tag, ReadOnlySpan<byte> data, bool clockIsUtc)
    {
        return tag.Type switch
        {
            PlcDataType.Bool => (data[0] & (1 << tag.Bit)) != 0,
            PlcDataType.Byte => data[0],
            PlcDataType.Word => BinaryPrimitives.ReadUInt16BigEndian(data),
            PlcDataType.DWord => BinaryPrimitives.ReadUInt32BigEndian(data),
            PlcDataType.Int => BinaryPrimitives.ReadInt16BigEndian(data),
            PlcDataType.DInt => BinaryPrimitives.ReadInt32BigEndian(data),
            PlcDataType.Real => BinaryPrimitives.ReadSingleBigEndian(data),
            PlcDataType.LReal => BinaryPrimitives.ReadDoubleBigEndian(data),
            PlcDataType.String => ReadString(data),
            PlcDataType.Dtl => ReadDtlRaw(data) is { } dtl ? ToUtc(dtl, clockIsUtc) : null,
            _ => throw new NotSupportedException($"Formato {tag.Type} não suportado.")
        };
    }

    /// <summary>STRING S7: [0] tamanho máximo, [1] tamanho atual, [2..] caracteres.</summary>
    public static string ReadString(ReadOnlySpan<byte> data)
    {
        var maxLength = data[0];
        var currentLength = data[1];
        var length = Math.Min(currentLength, Math.Min(maxLength, data.Length - 2));
        return Encoding.Latin1.GetString(data.Slice(2, length)).TrimEnd('\0', ' ');
    }

    /// <summary>
    /// DTL S7: ano(2) mês dia diaSemana hora min seg nanossegundos(4).
    /// Retorna null se os bytes não formarem uma data válida (ex.: DTL zerado).
    /// </summary>
    public static DateTime? ReadDtlRaw(ReadOnlySpan<byte> data)
    {
        var year = BinaryPrimitives.ReadUInt16BigEndian(data);
        var month = data[2];
        var day = data[3];
        var hour = data[5];
        var minute = data[6];
        var second = data[7];
        var nanoseconds = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(8));

        if (year is < 1 or > 9999 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)
            || hour > 23 || minute > 59 || second > 59 || nanoseconds > 999_999_999)
        {
            return null;
        }

        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified)
            .AddTicks(nanoseconds / 100);
    }

    /// <summary>Converte a hora do relógio do CLP para UTC.</summary>
    public static DateTime ToUtc(DateTime plcTime, bool clockIsUtc)
    {
        return clockIsUtc
            ? DateTime.SpecifyKind(plcTime, DateTimeKind.Utc)
            : TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(plcTime, DateTimeKind.Unspecified), TimeZoneInfo.Local);
    }
}
