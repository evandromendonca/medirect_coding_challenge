using MeDirectCurrencyExchange.Api.DTO;
using MeDirectCurrencyExchange.Api.Exceptions;
using MeDirectCurrencyExchange.Api.Extensions;
using MeDirectCurrencyExchange.Api.Models;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace MeDirectCurrencyExchange.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TradesController : ControllerBase
{
    private readonly ILogger<TradesController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _distributedCache;

    public TradesController(ILogger<TradesController> logger, IUnitOfWork unitOfWork, IDistributedCache distributedCache)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _distributedCache = distributedCache;
    }

    [HttpGet("{clientId:int}")]
    public async Task<ActionResult<IEnumerable<TradeDTO>>> GetTradesByClient(int clientId, DateTimeOffset from)
    {
        if (from > DateTimeOffset.UtcNow) throw new ArgumentException("Date from cannot be in the future");

        IEnumerable<Trade> trades = await _unitOfWork.Trades.GetTradesByClientIdAsync(clientId, from);

        return Ok(trades.Select(o => new TradeDTO(o)));
    }

    [HttpPost]
    public async Task<ActionResult<TradeDTO>> TradeCurrency(TradeParamsDTO tradeParams)
    {
        _logger.LogInformation($"Starting trading pair {tradeParams.BaseCurrency}/{tradeParams.TargetCurrency} " +
            $"for client {tradeParams.ClientId}");

        // check if rate is valid
        Rate? rate = await _unitOfWork.Rates
            .GetLatestClientPairRate(tradeParams.BaseCurrency, tradeParams.TargetCurrency, tradeParams.ClientId);

        if (rate == null)
            return NotFound($"No rate from the pair {tradeParams.BaseCurrency}/{tradeParams.TargetCurrency} " +
                $"found for the client {tradeParams.ClientId}, please request a rate");

        if (!rate.IsValid())
            throw new InvalidRateException(rate, "Rate older than 30 minutes, please request a new rate.");

        if (rate.Value != tradeParams.ExpectedRate)
            throw new InvalidRateException(rate, $"Rate value is different from client's last rate for the " +
                $"pair {tradeParams.BaseCurrency}/{tradeParams.TargetCurrency}. " +
                $"Expected value: {tradeParams.ExpectedRate}. Latest rate value: {rate.Value}");

        _logger.LogInformation("Rate is valid");

        // check if can trade
        TradeLimiter tradeLimiter = await GetTradeLimiterAsync(tradeParams);

        // create new trade
        Trade trade = new(rate, tradeParams.ClientId, tradeParams.BalanceBaseCurrency, tradeParams.Fees);

        if (!tradeLimiter.AddTrade(trade))
        {
            _logger.LogInformation($"Client {tradeParams.ClientId} trade limit exceeded. " +
                $"Next available trading time: {tradeLimiter.GetNextAvailableTradeTime()}");
            return StatusCode(StatusCodes.Status429TooManyRequests, "Trade limit exceeded. The limit is 10 trades per hour. " +
                $"Next available trading time: {tradeLimiter.GetNextAvailableTradeTime()}");
        }

        _logger.LogInformation("Client within trading limit, saving trade");

        // update trade limited in cache, ttl 1 hour
        await _distributedCache.SetInCacheAsync(tradeLimiter.CacheKey, tradeLimiter, 60 * 60);

        await _unitOfWork.Trades.AddAsync(trade);
        await _unitOfWork.SaveAsync();

        return Ok(new TradeDTO(trade));
    }

    #region private methods

    private async Task<TradeLimiter> GetTradeLimiterAsync(TradeParamsDTO tradeParams)
    {
        _logger.LogInformation("Getting trade limiter");

        TradeLimiter? tradeLimiter = await _distributedCache
            .GetFromCacheAsync<TradeLimiter>($"{tradeParams.ClientId}_trade_limiter");

        if (tradeLimiter == null)
        {
            _logger.LogInformation("Trade limiter cache miss, getting client's latest 10 trades to create new limiter");

            DateTimeOffset oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1);

            // get last trades from client from db
            var trades = await _unitOfWork.Trades.GetLatestTradesByClientAsync(tradeParams.ClientId, 10, oneHourAgo);

            // build trade limiter
            tradeLimiter = new($"{tradeParams.ClientId}_trade_limiter", trades);

            _logger.LogInformation("Trade limiter built, caching for 1 hour");

            // save in cache, ttl 1 hour
            await _distributedCache.SetInCacheAsync(tradeLimiter.CacheKey, tradeLimiter, 60 * 60);
        }

        return tradeLimiter;

    }

    #endregion
}