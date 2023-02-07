using MeDirectCurrencyExchange.RateProvider.Models;
using MeDirectCurrencyExchange.RateProvider.Exceptions;

namespace MeDirectCurrencyExchange.RateProvider.Interfaces;

public interface IRateProvider
{
    /// <summary>
    /// Get live quote for currency pair
    /// </summary>
    /// <param name="baseCurrency">Base currency</param>
    /// <param name="targetCurrency">Target currency</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="RateProviderException"></exception>
    Task<ProviderRateResult> GetCurrentRateAsync(string baseCurrency, string targetCurrency);
}
