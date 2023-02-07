using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using MeDirectCurrencyExchange.RateProvider.Exceptions;
using MeDirectCurrencyExchange.Api.Exceptions;

namespace MeDirectCurrencyExchange.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, ex.Message);

            ProblemDetails problemDetails = new()
            {
                Detail = ex.Message,
                Title = "One or more arguments are invalid",
                Status = StatusCodes.Status200OK,
            };

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = Application.Json;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
        catch (InvalidRateException ex)
        {
            _logger.LogWarning(ex, ex.Message);

            ProblemDetails problemDetails = new()
            {
                Detail = $"Rate: {ex.BaseCurrency}/{ex.TargetCurrency} @ {ex.Value} is invalid. Mesasge: {ex.Message}",
                Title = "Used rate is invalid",
                Status = StatusCodes.Status200OK,
            };

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = Application.Json;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
        catch (RateProviderException ex)
        {
            _logger.LogWarning(ex, ex.Message);

            ProblemDetails problemDetails = new()
            {
                Detail = "The system was unable to get a rate from the provider",
                Title = "Error from Rate Provider",
                Status = ex.Code != 0 ? ex.Code : StatusCodes.Status200OK,
            };

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = Application.Json;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            ProblemDetails problemDetails = new()
            {
                Detail = "An internal server error has occurred, please contact support",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
            };

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = Application.Json;

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
