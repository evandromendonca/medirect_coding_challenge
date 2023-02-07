using System.Text.Json.Serialization;

namespace MeDirectCurrencyExchange.Api.Models;

public class TradeLimiter
{
    public Queue<DateTimeOffset> Queue { get; set; } = new Queue<DateTimeOffset>();

    [JsonInclude]
    public string CacheKey { get; private set; }

    public TradeLimiter() { }

    public TradeLimiter(string cacheKey, IEnumerable<Trade> trades)
    {
        CacheKey = cacheKey;

        if (trades.Count() > 10)
            throw new Exception("Maximum accepted are 10 trades");

        foreach (var item in trades.OrderBy(o => o.DateCreated))
            Queue.Enqueue(item.DateCreated);

        Cleanup();
    }

    public bool AddTrade(Trade trade)
    {
        Cleanup();

        if (!CanTrade())
            return false;

        Queue.Enqueue(trade.DateCreated);

        return true;
    }

    public DateTimeOffset GetNextAvailableTradeTime()
    {
        if (Queue.Count < 10)
            return DateTimeOffset.UtcNow;

        DateTimeOffset firstTrade = Queue.Peek();

        return firstTrade.AddHours(1);
    }

    private bool CanTrade()
    {
        return Queue.Count < 10;
    }

    private void Cleanup()
    {
        if (Queue.Count == 0) return;

        DateTimeOffset oldestTrade = Queue.Peek();

        while (Queue.Count > 0 && (DateTimeOffset.UtcNow - oldestTrade).TotalHours > 1)
        {
            Queue.Dequeue();
            Queue.TryPeek(out oldestTrade);
        }
    }
}
