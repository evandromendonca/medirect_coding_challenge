using MeDirectCurrencyExchange.Api.Models;

namespace MeDirectCurrencyExchange.Api.Repositories.Interfaces;

public interface IRateRepository : IGenericRepository<Rate> {
    Task<Rate?> GetLatestClientPairRate(string baseCurrency, string targetCurrency, int clientId);
}
