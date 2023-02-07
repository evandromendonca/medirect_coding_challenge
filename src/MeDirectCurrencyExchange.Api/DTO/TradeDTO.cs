using MeDirectCurrencyExchange.Api.Models;

namespace MeDirectCurrencyExchange.Api.DTO;

public class TradeDTO
{
    public int TradeId { get; set; }
    public string BaseCurrency { get; set; }
    public decimal BaseAmount { get; set; }
    public string TargetCurrency { get; set; }
    public decimal TargetAmount { get; set; }
    public DateTimeOffset TradeTimestamp { get; set; }

    public TradeDTO(Trade trade)
    {
        TradeId = trade.Id;
        BaseCurrency = trade.BaseCurrency;
        BaseAmount = trade.BaseCurrencyAmount;
        TargetCurrency = trade.TargetCurrency;
        TargetAmount = trade.TargetCurrencyAmount;
        TradeTimestamp = trade.DateCreated;
    }
}
