namespace MeDirectCurrencyExchange.Api.DTO;

public class TradeParamsDTO
{
    public int ClientId { get; set; }
    public string BaseCurrency { get; set; }
    public string TargetCurrency { get; set; }
    public decimal ExpectedRate { get; set; }
    public decimal BalanceBaseCurrency { get; set; }    
    public decimal Fees { get; set; }
}
