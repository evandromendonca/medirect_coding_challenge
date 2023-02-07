using System.Globalization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MeDirectCurrencyExchante.RateProvider.Test")]
namespace MeDirectCurrencyExchange.RateProvider.Utils;

internal static class CurrencyCodeChecker
{
    static readonly HashSet<string> IsoCurrencySymbols = new();

    static CurrencyCodeChecker()
    {
        IsoCurrencySymbols = CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(o => new RegionInfo(o.LCID).ISOCurrencySymbol)
            .Where(o => !string.IsNullOrWhiteSpace(o)).ToHashSet();
    }

    public static bool ValidateCurrencySymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return false;

        if (symbol.Length != 3)
            return false;

        if (!IsoCurrencySymbols.Contains(symbol.ToUpper()))
            return false;

        return true;
    }
}