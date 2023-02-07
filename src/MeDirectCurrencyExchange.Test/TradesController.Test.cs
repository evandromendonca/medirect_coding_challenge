using MeDirectCurrencyExchange.Api.Controllers;
using MeDirectCurrencyExchange.Api.DTO;
using MeDirectCurrencyExchange.Api.Exceptions;
using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MeDirectCurrencyExchange.Api.Test;

public class TradesControllerTest
{
    [Fact]
    public async Task GetTradesByClient_5Existing_Returns()
    {
        // arrange           
        Rate rate = new()
        {
            ClientId = 1,
            BaseCurrency = "EUR",
            TargetCurrency = "USD",
            Value = 1.08M,
            RateTimestamp = DateTime.Now
        };

        List<Trade> tradesFromDb = new();
        for (int i = 0; i < 5; i++)
            tradesFromDb.Add(new(rate, 5, 100, 0));

        // mock db access
        Mock<ITradeRepository> mockITradeRepository = new();
        mockITradeRepository
              .Setup(o => o.GetTradesByClientIdAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
              .ReturnsAsync((int id, DateTimeOffset from) => tradesFromDb.Where(o => o.ClientId == id && o.DateCreated > from));
        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Trades).Returns(mockITradeRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        // act
        var result = await tradesController.GetTradesByClient(5, DateTimeOffset.UtcNow.AddHours(-5));

        // assert            
        Assert.NotNull(result.Result);
        Assert.IsType<ActionResult<IEnumerable<TradeDTO>>>(result);
        Assert.Equal(5, ((result.Result as ObjectResult)!.Value as IEnumerable<TradeDTO>)!.Count());
    }

    [Fact]
    public async Task GetTradesByClient_FutureFrom_ThrowsArgumentException()
    {
        // arrange           
        Rate rate = new()
        {
            ClientId = 1,
            BaseCurrency = "EUR",
            TargetCurrency = "USD",
            Value = 1.08M,
            RateTimestamp = DateTime.Now
        };

        List<Trade> tradesFromDb = new();
        for (int i = 0; i < 5; i++)
            tradesFromDb.Add(new(rate, 5, 100, 0));

        // mock db access
        Mock<ITradeRepository> mockITradeRepository = new();
        mockITradeRepository
              .Setup(o => o.GetTradesByClientIdAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
              .ReturnsAsync((int id, DateTimeOffset from) => tradesFromDb.Where(o => o.ClientId == id && o.DateCreated > from));
        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Trades).Returns(mockITradeRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        // act/assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await tradesController.GetTradesByClient(5, DateTimeOffset.UtcNow.AddHours(1)));

        Assert.Equal("Date from cannot be in the future", exception.Message);
    }

    [Fact]
    public async Task GetTradesByClient_NoneExisting_ReturnsEmpty()
    {
        // arrange           
        Rate rate = new()
        {
            ClientId = 1,
            BaseCurrency = "EUR",
            TargetCurrency = "USD",
            Value = 1.08M,
            RateTimestamp = DateTime.Now
        };

        List<Trade> tradesFromDb = new();
        for (int i = 0; i < 5; i++)
            tradesFromDb.Add(new(rate, 5, 100, 0));

        // mock db access
        Mock<ITradeRepository> mockITradeRepository = new();
        mockITradeRepository
              .Setup(o => o.GetTradesByClientIdAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
              .ReturnsAsync((int id, DateTimeOffset from) => tradesFromDb.Where(o => o.ClientId == id && o.DateCreated > from));
        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Trades).Returns(mockITradeRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        // act
        var result = await tradesController.GetTradesByClient(7, DateTimeOffset.UtcNow.AddHours(-5));

        // assert            
        Assert.NotNull(result.Result);
        Assert.IsType<ActionResult<IEnumerable<TradeDTO>>>(result);
        Assert.Empty((result.Result as ObjectResult)!.Value as IEnumerable<TradeDTO>);
    }

    [Fact]
    public async Task TradeCurrency_RateNotFound_ReturnNotFound()
    {
        // arrange           
        List<Rate> ratesFromDb = new()
        {
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "EUR",
                TargetCurrency = "USD",
                Value = 1.08M,
                RateTimestamp = DateTime.Now
            },
            new Rate()
            {
                ClientId = 2,
                BaseCurrency = "AUD",
                TargetCurrency = "EUR",
                Value = 0.69M,
                RateTimestamp = DateTime.Now
            },
            new Rate()
            {
                ClientId = 3,
                BaseCurrency = "BRL",
                TargetCurrency = "USD",
                Value = 0.19M,
                RateTimestamp = DateTime.Now
            }
        };

        // mock db access
        Mock<IRateRepository> mockIRateRepository = new();
        mockIRateRepository
              .Setup(o => o.GetLatestClientPairRate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
              .ReturnsAsync((string baseCurr, string targetCurr, int clientId) =>
                  ratesFromDb.Where(o => o.BaseCurrency == baseCurr && o.TargetCurrency == targetCurr && o.ClientId == clientId).FirstOrDefault()
              );
        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Rates).Returns(mockIRateRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        // act
        var result = await tradesController.TradeCurrency(new TradeParamsDTO()
        {
            BalanceBaseCurrency = 100,
            BaseCurrency = "EUR",
            ClientId = 4,
            ExpectedRate = 0.64M,
            Fees = 0,
            TargetCurrency = "USD"
        });

        // assert            
        Assert.NotNull(result.Result);
        Assert.Equal(404, (result.Result as ObjectResult)!.StatusCode);
    }

    [Fact]
    public async Task TradeCurrency_RateOlderThan30Minutes_InvalidRateException()
    {
        // arrange           
        List<Rate> ratesFromDb = new()
        {
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "EUR",
                TargetCurrency = "USD",
                Value = 1.08M,
                RateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-40)
            },
        };

        // mock db access
        Mock<IRateRepository> mockIRateRepository = new();
        mockIRateRepository
              .Setup(o => o.GetLatestClientPairRate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
              .ReturnsAsync((string baseCurr, string targetCurr, int clientId) =>
                  ratesFromDb.Where(o => o.BaseCurrency == baseCurr && o.TargetCurrency == targetCurr && o.ClientId == clientId).FirstOrDefault()
              );
        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Rates).Returns(mockIRateRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        // act/assert
        var exception = await Assert.ThrowsAsync<InvalidRateException>(async () => await tradesController.TradeCurrency(new TradeParamsDTO()
        {
            BalanceBaseCurrency = 100,
            BaseCurrency = "EUR",
            ClientId = 1,
            ExpectedRate = 1.08M,
            Fees = 0,
            TargetCurrency = "USD"
        }));

        Assert.Equal("Rate older than 30 minutes, please request a new rate.", exception.Message);
    }

    [Fact]
    public async Task TradeCurrency_RateValueDifferentThanExpected_InvalidRateException()
    {
        // arrange           
        List<Rate> ratesFromDb = new()
        {
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "EUR",
                TargetCurrency = "USD",
                Value = 1.08M,
                RateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
        };

        // mock db access
        Mock<IRateRepository> mockIRateRepository = new();
        mockIRateRepository
              .Setup(o => o.GetLatestClientPairRate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
              .ReturnsAsync((string baseCurr, string targetCurr, int clientId) =>
                  ratesFromDb.Where(o => o.BaseCurrency == baseCurr && o.TargetCurrency == targetCurr && o.ClientId == clientId).FirstOrDefault()
              );
        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Rates).Returns(mockIRateRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        // act/assert
        var exception = await Assert.ThrowsAsync<InvalidRateException>(async () => await tradesController.TradeCurrency(new TradeParamsDTO()
        {
            BalanceBaseCurrency = 100,
            BaseCurrency = "EUR",
            ClientId = 1,
            ExpectedRate = 1.15M,
            Fees = 0,
            TargetCurrency = "USD"
        }));

        Assert.StartsWith("Rate value is different from client's last rate for the pair", exception.Message);
    }

    [Fact]
    public async Task TradeCurrency_TradeLimitExceeded_ReturnStatus429()
    {
        // arrange           
        List<Rate> ratesFromDb = new()
        {
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "EUR",
                TargetCurrency = "USD",
                Value = 1.08M,
                RateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
        };

        List<Trade> tradesFromDb = new();

        // mock db access
        Mock<IRateRepository> mockIRateRepository = new();
        mockIRateRepository
              .Setup(o => o.GetLatestClientPairRate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
              .ReturnsAsync((string baseCurr, string targetCurr, int clientId) =>
                  ratesFromDb.Where(o => o.BaseCurrency == baseCurr && o.TargetCurrency == targetCurr && o.ClientId == clientId).FirstOrDefault()
              );
        Mock<ITradeRepository> mockITradeRepository = new();
        mockITradeRepository
              .Setup(o => o.GetLatestTradesByClientAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
              .ReturnsAsync((int clientId, int maxTradeCount, DateTimeOffset from)
                => tradesFromDb.Where(o => o.ClientId == clientId && o.DateCreated > from).OrderByDescending(o => o.DateCreated).Take(maxTradeCount));

        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Rates).Returns(mockIRateRepository.Object);
        mockIUnitOfWork.Setup(o => o.Trades).Returns(mockITradeRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        TradeParamsDTO tradeParams = new()
        {
            BalanceBaseCurrency = 100,
            BaseCurrency = "EUR",
            ClientId = 1,
            ExpectedRate = 1.08M,
            Fees = 0,
            TargetCurrency = "USD"
        };

        // act
        for (int i = 0; i < 10; i++)
        {
            await tradesController.TradeCurrency(tradeParams);
        }

        var result = await tradesController.TradeCurrency(tradeParams);

        // assert
        Assert.NotNull(result.Result);
        Assert.Equal(429, (result.Result as ObjectResult)!.StatusCode);
        Assert.StartsWith("Trade limit exceeded. The limit is 10 trades per hour. Next available trading time:", ((result.Result as ObjectResult)!.Value as string));
    }

    [Fact]
    public async Task TradeCurrency_RateValid_WithinLimit_ReturnTradeDTO()
    {
        // arrange           
        List<Rate> ratesFromDb = new()
        {
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "EUR",
                TargetCurrency = "USD",
                Value = 1.08M,
                RateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
        };

        List<Trade> tradesFromDb = new();

        // mock db access
        Mock<IRateRepository> mockIRateRepository = new();
        mockIRateRepository
              .Setup(o => o.GetLatestClientPairRate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
              .ReturnsAsync((string baseCurr, string targetCurr, int clientId) =>
                  ratesFromDb.Where(o => o.BaseCurrency == baseCurr && o.TargetCurrency == targetCurr && o.ClientId == clientId).FirstOrDefault()
              );
        Mock<ITradeRepository> mockITradeRepository = new();
        mockITradeRepository
              .Setup(o => o.GetLatestTradesByClientAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>()))
              .ReturnsAsync((int clientId, int maxTradeCount, DateTimeOffset from)
                => tradesFromDb.Where(o => o.ClientId == clientId && o.DateCreated > from).OrderByDescending(o => o.DateCreated).Take(maxTradeCount));

        Mock<IUnitOfWork> mockIUnitOfWork = new();
        mockIUnitOfWork.Setup(o => o.Rates).Returns(mockIRateRepository.Object);
        mockIUnitOfWork.Setup(o => o.Trades).Returns(mockITradeRepository.Object);

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<TradesController> logger = new();

        TradesController tradesController = new(logger, mockIUnitOfWork.Object, distributedCache);

        TradeParamsDTO tradeParams = new()
        {
            BalanceBaseCurrency = 100,
            BaseCurrency = "EUR",
            ClientId = 1,
            ExpectedRate = 1.08M,
            Fees = 0,
            TargetCurrency = "USD"
        };

        // act       
        var result = await tradesController.TradeCurrency(tradeParams);

        // assert
        Assert.NotNull(result.Result);
        Assert.IsType<ActionResult<TradeDTO>>(result);
        Assert.Equal(92.59M, Math.Round(((result.Result as ObjectResult)!.Value as TradeDTO)!.TargetAmount, 2));
    }
}