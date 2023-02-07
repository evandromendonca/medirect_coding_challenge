using MeDirectCurrencyExchange.RateProvider.Exceptions;
using MeDirectCurrencyExchange.RateProvider.Implementations.Fixer.Models;
using MeDirectCurrencyExchange.RateProvider.Interfaces;
using MeDirectCurrencyExchange.RateProvider.Models;
using MeDirectCurrencyExchange.RateProvider.Options;
using MeDirectCurrencyExchange.RateProvider.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MeDirectCurrencyExchange.RateProvider.Implementations.Fixer;

public class FixerRateProvider : IRateProvider
{
    private readonly ILogger<FixerRateProvider> _logger;
    private readonly HttpClient _httpClient;

    public FixerRateProvider(ILogger<FixerRateProvider> logger,
        HttpClient httpClient, IOptions<RateProviderKeyOptions> options)
    {
        _logger = logger;

        string apiKey = options.Value.Fixer;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Missing Fixer api key");

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.apilayer.com/fixer/");
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

            _logger.LogInformation($"Requesting pair {baseCurrency}/{targetCurrency} rate from Fixer");

            var response = await _httpClient.GetAsync($"latest?base={baseCurrency}&symbols={targetCurrency}");

            var stringContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Reponse from Fixer: {stringContent}");

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

        var fixerResponse = JsonSerializer
            .Deserialize<FixerLatestResponse>(jsonString, serializerOptions);

        if (fixerResponse == null)
            throw new Exception($"Unexpected error, Fixer response could " +
                $"not be deserialized. Json response: {jsonString}.");

        if (!fixerResponse.Success)
            if (fixerResponse.Error != null)
                throw new RateProviderException(fixerResponse.Error.Info,
                    fixerResponse.Error.Code, fixerResponse.Error.Type);
            else
                throw new Exception($"Unexpected error, Fixer response could " +
                    $"not be deserialized. Json response: {jsonString}.");

        return new ProviderRateResult()
        {
            BaseCurrency = fixerResponse.Base,
            TargetCurrency = fixerResponse.Rates.SingleOrDefault().Key,
            Time = DateTimeOffset.FromUnixTimeSeconds(fixerResponse.Timestamp),
            Value = fixerResponse.Rates.SingleOrDefault().Value,
            RateProviderName = "Fixer"
        };
    }
}