using MeDirectCurrencyExchange.Api.Data;
using MeDirectCurrencyExchange.Api.Middlewares;
using MeDirectCurrencyExchange.Api.Repositories;
using MeDirectCurrencyExchange.Api.Repositories.Interfaces;
using MeDirectCurrencyExchange.RateProvider;
using MeDirectCurrencyExchange.RateProvider.Implementations.ExchangeRateDataApi;
using MeDirectCurrencyExchange.RateProvider.Implementations.Fixer;
using MeDirectCurrencyExchange.RateProvider.Interfaces;
using MeDirectCurrencyExchange.RateProvider.Options;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Cryptography;

namespace MeDirectCurrencyExchange.Api;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // configure and add logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Services.AddLogging(config => config.AddSerilog());

            // rate providers options (keys)
            builder.Services.Configure<RateProviderKeyOptions>(
                builder.Configuration.GetSection("RateProviderApiKeys"));

            // client
            builder.Services.AddHttpClient();

            // db context
            builder.Services.AddDbContext<CurrencyExchangeContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("CurrencyExchangeDatabase")));

            // Add services to the container.
            builder.Services.AddStackExchangeRedisCache(setup =>
            {
                setup.Configuration = builder.Configuration.GetConnectionString("Redis");
            });

            // global exception handler
            builder.Services.AddScoped<GlobalExceptionHandlerMiddleware>();

            // repositories and decorators
            builder.Services.AddScoped<UnitOfWork>();
            builder.Services.AddScoped<IUnitOfWork, CachedUnitOfWork>();

            // rate provider
            builder.Services.AddScoped<FixerRateProvider>();
            builder.Services.AddScoped<ExchangeRateDataApiProvider>();
            builder.Services.AddTransient<Func<ProviderEnum, IRateProvider>>(rateProvider => key =>
            {
                return key switch
                {
                    ProviderEnum.Fixer => rateProvider.GetService<FixerRateProvider>()!,
                    ProviderEnum.ExchangeRatesDataApi => rateProvider.GetService<ExchangeRateDataApiProvider>()!,
                    _ => null!,
                };
            });

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}