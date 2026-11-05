using System.Globalization;

namespace DataManager.Service.Models;

/// <summary>Tag do CLP já validada e pronta para leitura.</summary>
public sealed record PlcTag(string Name, int Db, PlcDataType Type, int ByteOffset, int Bit, int Size)
{
    private const int DefaultStringLength = 254;

    /// <summary>Valida uma <see cref="PlcTagDefinition"/> e calcula offset, bit e tamanho.</summary>
    public static PlcTag From(PlcTagDefinition def)
    {
        var where = $"tag '{def.Name}' (DB{def.Db}, endereço '{def.Address}')";

        if (string.IsNullOrWhiteSpace(def.Name))
            throw new InvalidOperationException($"Tag sem nome em DB{def.Db}, endereço '{def.Address}'.");

        if (def.Db <= 0)
            throw new InvalidOperationException($"Número de DB inválido na {where}.");

        if (string.IsNullOrWhiteSpace(def.Format)
            || int.TryParse(def.Format, out _)
            || !Enum.TryParse<PlcDataType>(def.Format.Trim(), ignoreCase: true, out var type)
            || !Enum.IsDefined(type))
        {
            var valid = string.Join(", ", Enum.GetNames<PlcDataType>()).ToUpperInvariant();
            throw new InvalidOperationException($"Formato '{def.Format}' inválido na {where}. Use: {valid}.");
        }

        var parts = (def.Address ?? string.Empty).Trim().Split('.');
        if (parts.Length > 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var byteOffset))
        {
            throw new InvalidOperationException($"Endereço inválido na {where}. Use \"byte\" ou \"byte.bit\".");
        }

        var bit = 0;
        if (parts.Length == 2
            && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out bit) || bit > 7))
        {
            throw new InvalidOperationException($"Bit inválido na {where}. Deve ser de 0 a 7.");
        }

        if (type != PlcDataType.Bool && bit != 0)
            throw new InvalidOperationException($"Endereço com bit só é permitido para BOOL ({where}).");

        if (type is PlcDataType.Word or PlcDataType.DWord or PlcDataType.Int or PlcDataType.DInt
                 or PlcDataType.Real or PlcDataType.LReal or PlcDataType.String or PlcDataType.Dtl
            && byteOffset % 2 != 0)
        {
            throw new InvalidOperationException($"Tipos maiores que 1 byte devem começar em endereço par no S7 ({where}).");
        }

        var stringLength = def.Length ?? DefaultStringLength;
        if (type == PlcDataType.String && stringLength is < 1 or > 254)
            throw new InvalidOperationException($"Length de STRING deve ser de 1 a 254 ({where}).");

        var size = type switch
        {
            PlcDataType.Bool or PlcDataType.Byte => 1,
            PlcDataType.Word or PlcDataType.Int => 2,
            PlcDataType.DWord or PlcDataType.DInt or PlcDataType.Real => 4,
            PlcDataType.LReal => 8,
            PlcDataType.String => stringLength + 2,
            PlcDataType.Dtl => 12,
            _ => throw new NotSupportedException($"Formato {type} não suportado.")
        };

        return new PlcTag(def.Name.Trim(), def.Db, type, byteOffset, bit, size);
    }
}
