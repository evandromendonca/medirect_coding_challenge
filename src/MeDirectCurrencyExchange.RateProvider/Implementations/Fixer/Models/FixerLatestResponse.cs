namespace MeDirectCurrencyExchange.RateProvider.Implementations.Fixer.Models;

internal class FixerLatestResponse
{
    public string Base { get; set; }
    public DateTime Date { get; set; }
    public Dictionary<string, decimal> Rates { get; set; } = new Dictionary<string, decimal>();
    public int Timestamp { get; set; }
    public bool Success { get; set; }
    public FixerError Error { get; set; }

    internal class FixerError
    {
        public string Type { get; set; }
        public int Code { get; set; }
        public string Info { get; set; }
    }
}
