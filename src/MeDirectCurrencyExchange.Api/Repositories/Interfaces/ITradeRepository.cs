using MeDirectCurrencyExchange.Api.Models;

namespace MeDirectCurrencyExchange.Api.Repositories.Interfaces;

public interface ITradeRepository : IGenericRepository<Trade>
{
    Task<IEnumerable<Trade>> GetTradesByClientIdAsync(int clientId, DateTimeOffset from);
    Task<IEnumerable<Trade>> GetLatestTradesByClientAsync(int clientId, int maxTradeCount = 10, DateTimeOffset? from = null);
}
