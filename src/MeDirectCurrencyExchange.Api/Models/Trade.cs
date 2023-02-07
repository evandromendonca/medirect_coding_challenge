namespace MeDirectCurrencyExchange.Api.Models;

public class Trade : BaseClass
{
    public int ClientId { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal BaseCurrencyAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal TargetCurrencyAmount { get; private set; }
    public int RateId { get; set; }

    private Trade() { }

    public Trade(Rate rate, int clientId, decimal baseCurrencyAmount, decimal fees)
    {
        ClientId = clientId;
        RateId = rate.Id;
        BaseCurrency = rate.BaseCurrency;
        TargetCurrency = rate.TargetCurrency;
        Rate = rate.Value;
        BaseCurrencyAmount = baseCurrencyAmount;
        Fees = fees;

        CalculateAmountConverted();
    }

    private void CalculateAmountConverted()
    {
        if (Rate <= 0) throw new Exception("Cannot convert currency with rate zero");

        TargetCurrencyAmount = (BaseCurrencyAmount * Rate) - Fees;
    }
}
