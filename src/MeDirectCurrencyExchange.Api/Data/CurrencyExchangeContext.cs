using MeDirectCurrencyExchange.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MeDirectCurrencyExchange.Api.Data;

public class CurrencyExchangeContext : DbContext
{
    public CurrencyExchangeContext(DbContextOptions<CurrencyExchangeContext> options)
        : base(options) { }

    public DbSet<Rate> Rates { get; set; }
    public DbSet<Trade> Trades { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.RateConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TradeConfiguration());
    }
}
