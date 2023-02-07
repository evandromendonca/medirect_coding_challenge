using MeDirectCurrencyExchange.Api.Models;

namespace MeDirectCurrencyExchange.Api.DTO;

public class RateDTO
{
    public int RateId { get; set; }
    public string BaseCurrency { get; set; }
    public string TargetCurrency { get; set; }
    public DateTimeOffset Time { get; set; }
    public decimal Rate { get; set; }

    public RateDTO(Rate rate)
    {
        RateId = rate.Id;
        BaseCurrency = rate.BaseCurrency;
        TargetCurrency = rate.TargetCurrency;
        Time = rate.RateTimestamp;
        Rate = rate.Value;
    }
}
