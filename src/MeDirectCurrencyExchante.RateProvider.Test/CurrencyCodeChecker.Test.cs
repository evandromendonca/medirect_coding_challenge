using MeDirectCurrencyExchange.RateProvider.Utils;

namespace MeDirectCurrencyExchante.RateProvider.Test
{
    public class CurrencyCodeCheckerTest
    {
        [Theory]
        [InlineData("EUR")]
        [InlineData("brl")]
        [InlineData("uSD")]
        [InlineData("JpY")]
        public void ValidateCurrencySymbol_ReturnsTrue(string code)
        {
            bool isValid = CurrencyCodeChecker.ValidateCurrencySymbol(code);

            Assert.True(isValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("BRLs")]
        [InlineData("US")]
        [InlineData("J")]
        [InlineData("E R")]
        public void ValidateCurrencySymbol_ReturnsFalse(string code)
        {
            bool isValid = CurrencyCodeChecker.ValidateCurrencySymbol(code);

            Assert.False(isValid);
        }
    }
}
