using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Repositories;

public class CachedTradeRepository : CachedGenericRepository<Trade>, ITradeRepository
{    
    private readonly ITradeRepository _decoratedTradeRepository;

    public CachedTradeRepository(ITradeRepository decoratedTradeRepository, IDistributedCache distributedCache)
        : base(decoratedTradeRepository, distributedCache)
    {
        _decoratedTradeRepository = decoratedTradeRepository;
    }

    public async Task<IEnumerable<Trade>> GetLatestTradesByClientAsync(int clientId, 
        int tradeCount = 10, DateTimeOffset? from = null)
    {
        return await _decoratedTradeRepository.GetLatestTradesByClientAsync(clientId, tradeCount);
    }

    public async Task<IEnumerable<Trade>> GetTradesByClientIdAsync(int clientId, DateTimeOffset from)
    {
        return await _decoratedTradeRepository.GetTradesByClientIdAsync(clientId, from);
    }

    public override async Task AddAsync(Trade entity)
    {
        await _decoratedTradeRepository.AddAsync(entity);
    }
}
