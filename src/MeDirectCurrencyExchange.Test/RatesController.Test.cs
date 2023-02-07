using MeDirectCurrencyExchange.Api.Controllers;
using MeDirectCurrencyExchange.Api.DTO;
using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using MeDirectCurrencyExchange.RateProvider;
using MeDirectCurrencyExchange.RateProvider.Interfaces;
using MeDirectCurrencyExchange.RateProvider.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace MeDirectCurrencyExchange.Api.Test;

public class RatesControllerTest
{
    [Theory]
    [InlineData("EUR", "USD", 1.08)]
    [InlineData("AUD", "EUR", 0.69)]
    [InlineData("BRL", "USD", 0.19)]
    public async Task GetRate_NoClient_RateNotInDB_ReturnRate(string baseCurr, string targetCurr, decimal expectedRate)
    {
        // arrange           
        List<Rate> ratesFromDb = new();

        ProviderRateResult rateFromProvider = new ProviderRateResult()
        {
            BaseCurrency = baseCurr,
            TargetCurrency = targetCurr,
            RateProviderName = "test",
            Time = DateTimeOffset.Now,
            Value = expectedRate
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

        // mock rate provider
        Mock<IRateProvider> mockRateProvider = new();
        mockRateProvider.Setup(o => o.GetCurrentRateAsync(baseCurr, targetCurr)).ReturnsAsync(rateFromProvider);

        // mock provider selector
        Func<ProviderEnum, IRateProvider> mockProviderSelector = (ProviderEnum providerEnum) => mockRateProvider.Object;

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<RatesController> logger = new();

        RatesController ratesController = new(logger, mockIUnitOfWork.Object, mockProviderSelector, distributedCache);

        // act
        var result = await ratesController.GetRate(baseCurr, targetCurr);

        // assert            
        Assert.NotNull(result.Result);
        Assert.IsType<ActionResult<RateDTO>>(result);
        Assert.Equal(expectedRate, ((result.Result as ObjectResult)!.Value as RateDTO)!.Rate);
    }

    [Theory]
    [InlineData("EUR", "USD", 1.08)]
    [InlineData("AUD", "EUR", 0.69)]
    [InlineData("BRL", "USD", 0.19)]
    public async Task GetRate_ClientOn_ClientRateInDB_ReturnRate(string baseCurr, string targetCurr, decimal expectedRate)
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
                ClientId = 1,
                BaseCurrency = "AUD",
                TargetCurrency = "EUR",
                Value = 0.69M,
                RateTimestamp = DateTime.Now
            },
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "BRL",
                TargetCurrency = "USD",
                Value = 0.19M,
                RateTimestamp = DateTime.Now
            }
        };

        ProviderRateResult rateFromProvider = new ProviderRateResult()
        {
            BaseCurrency = baseCurr,
            TargetCurrency = targetCurr,
            RateProviderName = "test",
            Time = DateTimeOffset.Now,
            Value = expectedRate
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

        // mock rate provider
        Mock<IRateProvider> mockRateProvider = new();
        mockRateProvider.Setup(o => o.GetCurrentRateAsync(baseCurr, targetCurr)).ReturnsAsync(rateFromProvider);

        // mock provider selector
        Func<ProviderEnum, IRateProvider> mockProviderSelector = (ProviderEnum providerEnum) => mockRateProvider.Object;

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

        // mock logger
        NullLogger<RatesController> logger = new();

        RatesController ratesController = new(logger, mockIUnitOfWork.Object, mockProviderSelector, distributedCache);

        // act
        var result = await ratesController.GetRate(baseCurr, targetCurr, 1);

        // assert            
        Assert.NotNull(result.Result);
        Assert.IsType<ActionResult<RateDTO>>(result);
        Assert.Equal(expectedRate, ((result.Result as ObjectResult)!.Value as RateDTO)!.Rate);
    }

    [Theory]
    [InlineData("EUR", "USD", 1.08)]
    [InlineData("AUD", "EUR", 0.69)]
    [InlineData("BRL", "USD", 0.19)]
    public async Task GetRate_ClientOn_ClientRateInDB_CacheHit_ReturnRate(string baseCurr, string targetCurr, decimal expectedRate)
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
                ClientId = 1,
                BaseCurrency = "AUD",
                TargetCurrency = "EUR",
                Value = 0.69M,
                RateTimestamp = DateTime.Now
            },
            new Rate()
            {
                ClientId = 1,
                BaseCurrency = "BRL",
                TargetCurrency = "USD",
                Value = 0.19M,
                RateTimestamp = DateTime.Now
            }
        };

        ProviderRateResult rateFromProvider = new ProviderRateResult()
        {
            BaseCurrency = baseCurr,
            TargetCurrency = targetCurr,
            RateProviderName = "test",
            Time = DateTimeOffset.UtcNow,
            Value = expectedRate
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

        // mock rate provider
        Mock<IRateProvider> mockRateProvider = new();
        mockRateProvider.Setup(o => o.GetCurrentRateAsync(baseCurr, targetCurr)).ReturnsAsync(rateFromProvider);

        // mock provider selector
        Func<ProviderEnum, IRateProvider> mockProviderSelector = (ProviderEnum providerEnum) => mockRateProvider.Object;

        // mock cache
        IDistributedCache distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        await distributedCache.SetStringAsync($"pair_{baseCurr}_{targetCurr}", JsonSerializer.Serialize(rateFromProvider));

        // mock logger
        NullLogger<RatesController> logger = new();

        RatesController ratesController = new(logger, mockIUnitOfWork.Object, mockProviderSelector, distributedCache);

        // act
        var result = await ratesController.GetRate(baseCurr, targetCurr, 1);

        // assert            
        Assert.NotNull(result.Result);
        Assert.IsType<ActionResult<RateDTO>>(result);
        Assert.Equal(expectedRate, ((result.Result as ObjectResult)!.Value as RateDTO)!.Rate);
    }
}