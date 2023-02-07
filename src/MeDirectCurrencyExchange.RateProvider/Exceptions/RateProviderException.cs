namespace MeDirectCurrencyExchange.RateProvider.Exceptions;

public class RateProviderException : Exception
{
    public int Code { get; set; }
    public string Type { get; set; }

    public RateProviderException(int code, string type)
    {
        Code = code;
        Type = type;
    }

    public RateProviderException(string? message, int code, string type) : base(message)
    {
        Code = code;
        Type = type;
    }

    public RateProviderException(string? message, int code, string type, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
        Type = type;
    }
}
