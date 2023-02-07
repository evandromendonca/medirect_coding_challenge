using MeDirectCurrencyExchange.Api.Models;

namespace MeDirectCurrencyExchange.Api.Exceptions
{
    public class InvalidRateException : Exception
    {
        public string BaseCurrency { get; set; }
        public string TargetCurrency { get; set; }
        public decimal Value { get; set; }

        public InvalidRateException(Rate rate)
        {
            BaseCurrency = rate.BaseCurrency;
            TargetCurrency = rate.TargetCurrency;
            Value = rate.Value;
        }

        public InvalidRateException(Rate rate, string? message)
            : base(message)
        {
            BaseCurrency = rate.BaseCurrency;
            TargetCurrency = rate.TargetCurrency;
            Value = rate.Value;
        }

        public InvalidRateException(Rate rate,
            string? message, Exception? innerException) : base(message, innerException)
        {
            BaseCurrency = rate.BaseCurrency;
            TargetCurrency = rate.TargetCurrency;
            Value = rate.Value;
        }
    }
}
