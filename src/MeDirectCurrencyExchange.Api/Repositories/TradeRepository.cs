using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeDirectCurrencyExchange.Api.Repositories;

public class TradeRepository : GenericRepository<Trade>, ITradeRepository
{
    private readonly DbContext _context;

    public TradeRepository(DbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Trade>> GetLatestTradesByClientAsync(int clientId, 
        int tradeCount = 10, DateTimeOffset? from = null)
    {
        var query = _context.Set<Trade>()
            .Where(o => o.ClientId == clientId);

        if (from != null)
            query = query.Where(o => o.DateCreated >= from.Value);

        return await query
            .OrderByDescending(o => o.Id)
            .Take(tradeCount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Trade>> GetTradesByClientIdAsync(int clientId, DateTimeOffset from)
    {
        return await _context.Set<Trade>()
            .Where(o => o.ClientId == clientId && o.DateCreated >= from)
            .ToListAsync();
    }
}
