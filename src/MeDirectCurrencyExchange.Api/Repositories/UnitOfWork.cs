using MeDirectCurrencyExchange.Api.Data;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    private readonly CurrencyExchangeContext _context;

    public UnitOfWork(CurrencyExchangeContext context)
    {
        _context = context;
    }

    public ITradeRepository Trades => new TradeRepository(_context);
    public IRateRepository Rates => new RateRepository(_context);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
            if (disposing)
                _context.Dispose();

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<int> SaveAsync()
    {
        return await _context.SaveChangesAsync();
    }
}