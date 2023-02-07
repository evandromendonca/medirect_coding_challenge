namespace MeDirectCurrencyExchange.Api.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITradeRepository Trades { get; }
    IRateRepository Rates { get; }
    Task<int> SaveAsync();
}
