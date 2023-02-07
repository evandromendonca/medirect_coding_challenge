using MeDirectCurrencyExchange.Api.Models;

namespace MeDirectCurrencyExchange.Api.Test;

public class TradeLimiterTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void CreateTradeLimiter_CorrectTradeCount_Returns(int tradesCount)
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        List<Trade> mockTrades = new();
        for (int i = 0; i < tradesCount; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        TradeLimiter limiter = new("testkey", mockTrades);

        Assert.Equal(tradesCount, limiter.Queue.Count);
    }

    [Theory]
    [InlineData(11)]
    public void CreateTradeLimiter_WrongTradeCount_Returns(int tradesCount)
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        List<Trade> mockTrades = new();
        for (int i = 0; i < tradesCount; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        var exception = Assert.Throws<Exception>(() => new TradeLimiter("testkey", mockTrades));

        Assert.Equal("Maximum accepted are 10 trades", exception.Message);
    }

    [Fact]
    public void CreateTradeLimiter_10Trades5Old_Returns()
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        List<Trade> mockTrades = new();
        for (int i = 0; i < 10; i++)
        {
            Trade trade = new(mockRate, 0, 100, 0);
            if (i < 5)
                trade.DateCreated = DateTimeOffset.UtcNow.AddHours(-2);
            mockTrades.Add(trade);
        }

        TradeLimiter limiter = new("testkey", mockTrades);

        Assert.Equal(5, limiter.Queue.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void AddToTradeLimiter_InLimit_ReturnsTrue(int tradesCount)
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        List<Trade> mockTrades = new();
        for (int i = 0; i < tradesCount; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        TradeLimiter limiter = new("testkey", mockTrades);

        var result = limiter.AddTrade(new Trade(mockRate, 1, 100, 0));

        Assert.True(result);
    }

    [Fact]
    public void AddToTradeLimiter_OffLimitsWith10Trades_ReturnsFalse()
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        List<Trade> mockTrades = new();
        for (int i = 0; i < 10; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        TradeLimiter limiter = new("testkey", mockTrades);

        var result = limiter.AddTrade(new Trade(mockRate, 1, 100, 0));

        Assert.False(result);
    }

    [Fact]
    public void AddToTradeLimiter_10Trades1Old_ReturnsTrue()
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        Trade oldTrade = new(mockRate, 0, 100, 0);
        oldTrade.DateCreated = DateTimeOffset.UtcNow.AddHours(-2);

        List<Trade> mockTrades = new() { oldTrade };

        for (int i = 0; i < 9; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        TradeLimiter limiter = new("testkey", mockTrades);

        var result = limiter.AddTrade(new Trade(mockRate, 1, 100, 0));

        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void GetNextAvailableTradeTime_InLimit_ReturnsNow(int tradesCount)
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        List<Trade> mockTrades = new();
        for (int i = 0; i < tradesCount; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        TradeLimiter limiter = new("testkey", mockTrades);

        var result = limiter.GetNextAvailableTradeTime();

        Assert.InRange((DateTimeOffset.UtcNow - result).TotalSeconds, 0, 1);
    }

    [Fact]
    public void GetNextAvailableTradeTime_OffLimits_ReturnsOneHourFromOldestTrade()
    {
        Rate mockRate = new()
        {
            BaseCurrency = "EUR",
            TargetCurrency = "AUD",
            Value = 1.03M,
        };

        Trade oldTrade = new(mockRate, 0, 100, 0);
        oldTrade.DateCreated = DateTimeOffset.UtcNow.AddMinutes(-20);

        // one hour from oldest date createad
        var oneHourFromOldest = oldTrade.DateCreated.AddHours(1);

        List<Trade> mockTrades = new() { oldTrade };

        for (int i = 0; i < 9; i++)
            mockTrades.Add(new Trade(mockRate, 0, 100, 0));

        TradeLimiter limiter = new("testkey", mockTrades);

        var result = limiter.GetNextAvailableTradeTime();

        Assert.Equal(oneHourFromOldest, result);
    }
}
