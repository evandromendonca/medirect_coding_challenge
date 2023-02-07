using MeDirectCurrencyExchange.RateProvider.Models;

namespace MeDirectCurrencyExchange.Api.Models;

public class Rate : BaseClass
{
    public int ClientId { get; set; }
    public string RateProvider { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTimeOffset RateTimestamp { get; set; }

    public bool IsValid()
    {
        if ((DateTimeOffset.UtcNow - RateTimestamp).TotalMinutes > 30)
            return false;

        return true;
    }
}
