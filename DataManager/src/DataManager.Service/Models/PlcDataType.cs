namespace DataManager.Service.Models;

/// <summary>Tipos de dados S7 suportados na leitura dos DBs.</summary>
public enum PlcDataType
{
    Bool,   // 1 bit  (endereço "byte.bit")
    Byte,   // 1 byte
    Word,   // 2 bytes, sem sinal
    DWord,  // 4 bytes, sem sinal
    Int,    // 2 bytes, com sinal
    DInt,   // 4 bytes, com sinal
    Real,   // 4 bytes, ponto flutuante
    LReal,  // 8 bytes, ponto flutuante
    String, // 2 bytes de cabeçalho + N caracteres
    Dtl     // 12 bytes, data/hora
}
