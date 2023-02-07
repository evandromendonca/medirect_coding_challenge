using MeDirectCurrencyExchange.RateProvider.Exceptions;
using MeDirectCurrencyExchange.RateProvider.Implementations.Fixer;
using MeDirectCurrencyExchange.RateProvider.Interfaces;
using MeDirectCurrencyExchange.RateProvider.Models;
using MeDirectCurrencyExchange.RateProvider.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace MeDirectCurrencyExchante.RateProvider.Test
{
    public class FixerRateProviderTest
    {
        [Theory]
        [InlineData("EUR", "USD", 1.08)]
        [InlineData("AUD", "EUR", 0.69)]
        [InlineData("BRL", "USD", 0.19)]
        public async Task GetCurrentRate_ReturnsRate(string baseCurrency, string targetCurrency, decimal expectedRate)
        {
            // arrange
            var mockHttp = new MockHttpMessageHandler();
            // json from Fixer documentation or live request
            mockHttp.When($"https://api.apilayer.com/fixer/latest?base={baseCurrency}&symbols={targetCurrency}")
                    .Respond("application/json", @$"{{
                        ""base"": ""{baseCurrency}"",
                        ""date"": ""2022-04-14"",
                        ""rates"": {{
                        ""{targetCurrency}"": {expectedRate}                      
                        }},
                        ""success"": true,
                        ""timestamp"": 1519296206
                    }}");
            var httpClient = mockHttp.ToHttpClient();

            NullLogger<FixerRateProvider> logger = new();

            var mockRateProviderKeyOption = new Mock<IOptions<RateProviderKeyOptions>>();
            mockRateProviderKeyOption.Setup(op => op.Value).Returns(new RateProviderKeyOptions()
            {
                Fixer = "testkey",
                ExchangeRatesDataApi = "testkey"
            });

            IRateProvider rateProvider = new FixerRateProvider(logger, httpClient, mockRateProviderKeyOption.Object);

            // act
            var rate = await rateProvider.GetCurrentRateAsync(baseCurrency, targetCurrency);

            // assert
            Assert.NotNull(rate);
            Assert.IsType<ProviderRateResult>(rate);
            Assert.Equal(expectedRate, rate.Value);
            Assert.Equal("Fixer", rate.RateProviderName);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("AUD", "XXX")]
        [InlineData("XXX", "AUD")]
        [InlineData("XXX", "XXX")]
        [InlineData("AUDS", "BRLS")]
        [InlineData("EUR", "EUR")]
        public async Task GetCurrentRate_InvalidCode_ThrowsArgumentException(string baseCurrency, string targetCurrency)
        {
            // arrange            
            // we should not hit any endpoint (to avoid spending requests)
            var mockHttp = new MockHttpMessageHandler();
            var httpClient = mockHttp.ToHttpClient();
            NullLogger<FixerRateProvider> logger = new();
            var mockRateProviderKeyOption = new Mock<IOptions<RateProviderKeyOptions>>();
            mockRateProviderKeyOption.Setup(op => op.Value).Returns(new RateProviderKeyOptions()
            {
                Fixer = "testkey",
                ExchangeRatesDataApi = "testkey"
            });

            IRateProvider rateProvider = new FixerRateProvider(logger, httpClient, mockRateProviderKeyOption.Object);

            // act/assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await rateProvider.GetCurrentRateAsync(baseCurrency, targetCurrency));
        }

        [Fact]
        public async Task GetCurrentRate_FixerFails_ThrowsRateProviderException()
        {
            // arrange            
            // will not define any endpoint
            var mockHttp = new MockHttpMessageHandler();
            var httpClient = mockHttp.ToHttpClient();
            NullLogger<FixerRateProvider> logger = new();
            var mockRateProviderKeyOption = new Mock<IOptions<RateProviderKeyOptions>>();
            mockRateProviderKeyOption.Setup(op => op.Value).Returns(new RateProviderKeyOptions()
            {
                Fixer = "testkey",
                ExchangeRatesDataApi = "testkey"
            });

            IRateProvider rateProvider = new FixerRateProvider(logger, httpClient, mockRateProviderKeyOption.Object);

            // act/assert
            var exception = await Assert.ThrowsAsync<RateProviderException>(
                async () => await rateProvider.GetCurrentRateAsync("EUR", "AUD"));

            Assert.Equal("Error in rate provider request", exception.Type);
            Assert.NotEqual(0, exception.Code);
        }

        [Fact]
        public async Task GetCurrentRate_ReceiveErrorJsonResponse_ThrowsRateProviderException()
        {
            // arrange                        
            var mockHttp = new MockHttpMessageHandler();

            // random unexpected json
            mockHttp.When($"https://api.apilayer.com/fixer/latest?base=EUR&symbols=AUD")
                   .Respond("application/json", @$"{{
                        ""success"": false,
                        ""error"": {{
                            ""code"": 201,
                            ""type"": ""invalid_base_currency""
                        }}
                    }}");

            var httpClient = mockHttp.ToHttpClient();
            NullLogger<FixerRateProvider> logger = new();
            var mockRateProviderKeyOption = new Mock<IOptions<RateProviderKeyOptions>>();
            mockRateProviderKeyOption.Setup(op => op.Value).Returns(new RateProviderKeyOptions()
            {
                Fixer = "testkey",
                ExchangeRatesDataApi = "testkey"
            });

            IRateProvider rateProvider = new FixerRateProvider(logger, httpClient, mockRateProviderKeyOption.Object);

            // act/assert
            var exception = await Assert.ThrowsAsync<RateProviderException>(
                async () => await rateProvider.GetCurrentRateAsync("EUR", "AUD"));

            Assert.Equal(201, exception.Code);
            Assert.Equal("invalid_base_currency", exception.Type);
        }

        [Fact]
        public async Task GetCurrentRate_ReceiveUnexpectedJson_ThrowsException()
        {
            // arrange                        
            var mockHttp = new MockHttpMessageHandler();

            // random unexpected json
            mockHttp.When($"https://api.apilayer.com/fixer/latest?base=EUR&symbols=AUD")
                   .Respond("application/json", @$"{{
                        ""this"": ""what?"",
                        ""is"": false,
                        ""unexpected"": 42
                    }}");

            var httpClient = mockHttp.ToHttpClient();
            NullLogger<FixerRateProvider> logger = new();
            var mockRateProviderKeyOption = new Mock<IOptions<RateProviderKeyOptions>>();
            mockRateProviderKeyOption.Setup(op => op.Value).Returns(new RateProviderKeyOptions()
            {
                Fixer = "testkey",
                ExchangeRatesDataApi = "testkey"
            });

            IRateProvider rateProvider = new FixerRateProvider(logger, httpClient, mockRateProviderKeyOption.Object);

            // act/assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await rateProvider.GetCurrentRateAsync("EUR", "AUD"));

            Assert.StartsWith("Unexpected error, Fixer response " +
                "could not be deserialized. Json response:", exception.Message);
        }

        [Fact]
        public void GetCurrentRate_NoApiKeys_ThrowArgumentException()
        {
            // arrange                        
            var mockHttp = new MockHttpMessageHandler();
            var httpClient = mockHttp.ToHttpClient();
            NullLogger<FixerRateProvider> logger = new();
            var mockRateProviderKeyOption = new Mock<IOptions<RateProviderKeyOptions>>();
            mockRateProviderKeyOption.Setup(op => op.Value).Returns(new RateProviderKeyOptions() { });

            // act/assert
            var exception = Assert.Throws<ArgumentException>(
                () => new FixerRateProvider(logger, httpClient, mockRateProviderKeyOption.Object));

            Assert.Equal("Missing Fixer api key", exception.Message);
        }
    }
}