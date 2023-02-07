using MeDirectCurrencyExchange.Api.DTO;
using MeDirectCurrencyExchange.Api.Extensions;
using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using MeDirectCurrencyExchange.RateProvider;
using MeDirectCurrencyExchange.RateProvider.Implementations.ExchangeRateDataApi;
using MeDirectCurrencyExchange.RateProvider.Implementations.Fixer;
using MeDirectCurrencyExchange.RateProvider.Interfaces;
using MeDirectCurrencyExchange.RateProvider.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RatesController : ControllerBase
{
    private readonly ILogger<RatesController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    Func<ProviderEnum, IRateProvider> _providerResolver;
    private readonly IDistributedCache _distributedCache;

    public RatesController(ILogger<RatesController> logger, IUnitOfWork unitOfWork,
        Func<ProviderEnum, IRateProvider> providerResolver, IDistributedCache distributedCache)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _providerResolver = providerResolver;
        _distributedCache = distributedCache;
    }

    [HttpGet("{baseCurrency:length(3)}/{targetCurrency:length(3)}")]
    public async Task<ActionResult<RateDTO>> GetRate(string baseCurrency, string targetCurrency, int? clientId = 0, string? preferredProvider = "fixer")
    {
        // fetch rate from provider        
        ProviderRateResult providerRate = await GetProviderRateAsync(baseCurrency, targetCurrency, preferredProvider);

        // get or create client rate
        Rate rate = await GetOrCreateClientRateAsync(providerRate, clientId);

        // return rate
        return Ok(new RateDTO(rate));
    }

    #region private methods

    private async Task<ProviderRateResult> GetProviderRateAsync(string baseCurrency, string targetCurrency, string? preferredProvider = "fixer")
    {
        string providerPairKey = $"pair_{baseCurrency}_{targetCurrency}";

        _logger.LogInformation($"Getting provider rate for {providerPairKey}");

        ProviderRateResult? providerRate = await _distributedCache.GetFromCacheAsync<ProviderRateResult>(providerPairKey);

        if (providerRate == null)
        {
            _logger.LogInformation("Cache miss, about to ask the rate provider for a rate");

            IRateProvider rateProvider = _providerResolver(preferredProvider switch
            {
                "exchange_rates_data_api" => ProviderEnum.ExchangeRatesDataApi,
                "fixer" => ProviderEnum.Fixer,
                _ => ProviderEnum.Fixer
            });

            providerRate = await rateProvider.GetCurrentRateAsync(baseCurrency, targetCurrency);

            _logger.LogInformation("Got rate from provider");

            await _distributedCache.SetInCacheAsync(providerPairKey, providerRate, 2 * 60); // 2 minutes ttl
        }

        return providerRate;
    }

    private async Task<Rate> GetOrCreateClientRateAsync(ProviderRateResult providerRate, int? clientId)
    {
        clientId ??= 0;

        _logger.LogInformation($"Getting client {clientId} rate for " +
            $"pair {providerRate.BaseCurrency}/{providerRate.TargetCurrency}");

        Rate? rate = await _unitOfWork.Rates
            .GetLatestClientPairRate(providerRate.BaseCurrency, providerRate.TargetCurrency, clientId.Value);

        if (rate == null || rate.RateTimestamp != providerRate.Time)
        {
            _logger.LogInformation("Rate not found or outdated, creating new rate");

            rate = new()
            {
                BaseCurrency = providerRate.BaseCurrency,
                TargetCurrency = providerRate.TargetCurrency,
                DateCreated = DateTimeOffset.UtcNow,
                RateProvider = providerRate.RateProviderName,
                RateTimestamp = providerRate.Time,
                Value = providerRate.Value,
                ClientId = clientId.Value,
            };

            await _unitOfWork.Rates.AddAsync(rate);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("New client rate created");
        }

        return rate;
    }

    #endregion
}