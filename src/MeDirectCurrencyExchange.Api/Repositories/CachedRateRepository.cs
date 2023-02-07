using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Repositories;

public class CachedRateRepository : CachedGenericRepository<Rate>, IRateRepository
{
    private readonly IRateRepository _decoratedRateRepository;

    public CachedRateRepository(IRateRepository decoratedRateRepository, IDistributedCache distributedCache)
        : base(decoratedRateRepository, distributedCache)
    {
        _decoratedRateRepository = decoratedRateRepository;
    }

    public override async Task AddAsync(Rate entity)
    {
        string key = $"pair_{entity.BaseCurrency}_{entity.TargetCurrency}_{entity.ClientId}";

        await _decoratedRateRepository.AddAsync(entity);

        await SetInCacheAsync(key, entity, 30 * 60);
    }

    public async Task<Rate?> GetLatestClientPairRate(string baseCurrency, string targetCurrency, int clientId)
    {
        string key = $"pair_{baseCurrency}_{targetCurrency}_{clientId}";

        var rate = await GetFromCacheAsync<Rate>(key);

        if (rate == null)
        {
            rate = await _decoratedRateRepository.GetLatestClientPairRate(baseCurrency, targetCurrency, clientId);
            if (rate != null)
                await SetInCacheAsync(key, rate);
        }

        return rate;
    }
}
