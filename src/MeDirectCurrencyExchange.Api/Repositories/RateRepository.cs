using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Repositories;

public class RateRepository : GenericRepository<Rate>, IRateRepository
{
    private readonly DbContext _context;

    public RateRepository(DbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Rate?> GetLatestClientPairRate(string baseCurrency, string targetCurrency, int clientId)
    {
        return await _context.Set<Rate>()
            .Where(o => o.ClientId == clientId && o.BaseCurrency == baseCurrency && o.TargetCurrency == targetCurrency)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();
    }
}