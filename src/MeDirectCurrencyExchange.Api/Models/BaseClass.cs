namespace MeDirectCurrencyExchange.Api.Models;

public abstract class BaseClass
{
    public int Id { get; set; }
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
}
