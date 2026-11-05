namespace DataManager.Service.Plc;

/// <summary>Erro de comunicação com o CLP (conexão ou leitura).</summary>
public sealed class PlcCommunicationException : Exception
{
    public int ErrorCode { get; }

    public PlcCommunicationException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
