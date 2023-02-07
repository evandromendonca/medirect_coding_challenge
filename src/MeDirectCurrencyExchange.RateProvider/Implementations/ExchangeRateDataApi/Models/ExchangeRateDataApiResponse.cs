using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeDirectCurrencyExchange.RateProvider.Implementations.ExchangeRateDataApi.Models;

internal class ExchangeRateDataApiResponse
{
    public string Base { get; set; }
    public DateTime Date { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = new Dictionary<string, decimal>();
    public int Timestamp { get; set; }
    public bool Success { get; set; }
    public ExchangeRateDataApiError Error { get; set; }

    internal class ExchangeRateDataApiError
    {
        public string Type { get; set; }
        public int Code { get; set; }
        public string Info { get; set; }
    }

}
