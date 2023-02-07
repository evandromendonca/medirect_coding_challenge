namespace MeDirectCurrencyExchange.RateProvider.Models;

public class ProviderRateResult
{
    public string RateProviderName { get; set; }
    public DateTimeOffset Time { get; set; }
    public decimal Value { get; set; }
    public string BaseCurrency { get; set; }
    public string TargetCurrency { get; set; }
}
