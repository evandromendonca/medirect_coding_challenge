using MeDirectCurrencyExchange.RateProvider.Interfaces;
using MeDirectCurrencyExchange.RateProvider.Models;
using MeDirectCurrencyExchange.RateProvider.Options;
using MeDirectCurrencyExchange.RateProvider.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using MeDirectCurrencyExchange.RateProvider.Exceptions;
using MeDirectCurrencyExchange.RateProvider.Implementations.ExchangeRateDataApi.Models;

namespace MeDirectCurrencyExchange.RateProvider.Implementations.ExchangeRateDataApi;

public class ExchangeRateDataApiProvider : IRateProvider
{
    private readonly ILogger<ExchangeRateDataApiProvider> _logger;
    private readonly HttpClient _httpClient;

    public ExchangeRateDataApiProvider(ILogger<ExchangeRateDataApiProvider> logger,
        HttpClient httpClient, IOptions<RateProviderKeyOptions> options)
    {
        _logger = logger;

        string apiKey = options.Value.ExchangeRatesDataApi;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Missing ExchangeRateDataApi api key");

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.apilayer.com/exchangerates_data/");
        _httpClient.DefaultRequestHeaders.Add("apiKey", apiKey);
    }

    /// <summary>
    /// Get live quote for currency pair
    /// </summary>
    /// <param name="baseCurrency">Base currency</param>
    /// <param name="targetCurrency">Target currency</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="RateProviderException"></exception>
    public async Task<ProviderRateResult> GetCurrentRateAsync(string baseCurrency, string targetCurrency)
    {
        try
        {
            // avoid fetching the api without correct symbols saving our quota
            if (!CurrencyCodeChecker.ValidateCurrencySymbol(baseCurrency))
                throw new ArgumentException($"Invalid currency code: '{baseCurrency}'.");

            if (!CurrencyCodeChecker.ValidateCurrencySymbol(targetCurrency))
                throw new ArgumentException($"Invalid currency code: '{targetCurrency}'.");

            baseCurrency = baseCurrency.ToUpper();
            targetCurrency = targetCurrency.ToUpper();

            if (baseCurrency == targetCurrency)
                throw new ArgumentException($"Base currency '{baseCurrency}' needs to " +
                    $"be different from target currency '{targetCurrency}'.");

            _logger.LogInformation($"Requesting pair {baseCurrency}/{targetCurrency} rate from ExchangeRateDataApi");

            var response = await _httpClient.GetAsync($"latest?base={baseCurrency}&symbols={targetCurrency}");

            var stringContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Reponse from ExchangeRateDataApi: {stringContent}");

            response.EnsureSuccessStatusCode();

            return ParseResponse(stringContent);
        }
        catch (HttpRequestException ex)
        {
            int statusCode = (int?)ex.StatusCode ?? 0;
            throw new RateProviderException(ex.Message, statusCode, "Error in rate provider request", ex);
        }
        catch (RateProviderException ex)
        {
            string message = $"Rate provider returned an error: {ex.Message}; code: {ex.Code}; type: {ex.Type}.";
            _logger.LogError(ex, message);
            throw;
        }
    }


    /// <summary>
    /// Parse the json data received from the rate provider to a ProviderRateResult object
    /// </summary>
    /// <param name="jsonString">Json string</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="RateProviderException"></exception>
    private static ProviderRateResult ParseResponse(string jsonString)
    {
        var serializerOptions = new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };

        var exchangeRateDataApiResponse = JsonSerializer
            .Deserialize<ExchangeRateDataApiResponse>(jsonString, serializerOptions);

        if (exchangeRateDataApiResponse == null)
            throw new Exception($"Unexpected error, Exchange Rate Data Api response could " +
                $"not be deserialized. Json response: {jsonString}.");

        if (!exchangeRateDataApiResponse.Success)
            if (exchangeRateDataApiResponse.Error != null)
                throw new RateProviderException(exchangeRateDataApiResponse.Error.Info,
                    exchangeRateDataApiResponse.Error.Code, exchangeRateDataApiResponse.Error.Type);
            else
                throw new Exception($"Unexpected error, Exchange Rate Data Api response could " +
                    $"not be deserialized. Json response: {jsonString}.");

        return new ProviderRateResult()
        {
            BaseCurrency = exchangeRateDataApiResponse.Base,
            TargetCurrency = exchangeRateDataApiResponse.Rates.SingleOrDefault().Key,
            Time = DateTimeOffset.FromUnixTimeSeconds(exchangeRateDataApiResponse.Timestamp),
            Value = exchangeRateDataApiResponse.Rates.SingleOrDefault().Value,
            RateProviderName = "Exchange Rate Data Api"
        };
    }
}
