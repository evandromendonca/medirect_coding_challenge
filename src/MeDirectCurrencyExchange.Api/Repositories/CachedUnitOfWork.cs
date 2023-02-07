using MeDirectCurrencyExchange.Api.Data;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Repositories;

public class CachedUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    private readonly UnitOfWork _decoratedUnitOfWork;
    private readonly IDistributedCache _distributedCache;

    public CachedUnitOfWork(UnitOfWork decoratedUnitOfWork, IDistributedCache distributedCache)
    {
        this._decoratedUnitOfWork = decoratedUnitOfWork;
        this._distributedCache = distributedCache;
    }

    public ITradeRepository Trades => new CachedTradeRepository(_decoratedUnitOfWork.Trades, _distributedCache);
    public IRateRepository Rates => new CachedRateRepository(_decoratedUnitOfWork.Rates, _distributedCache);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
            if (disposing)
                _decoratedUnitOfWork.Dispose();

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<int> SaveAsync()
    {
        return await _decoratedUnitOfWork.SaveAsync();
    }
}