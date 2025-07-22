using GuardianCapitalLLC.Controllers;
using GuardianCapitalLLC.Models;
using System.Net.Http;
using System.Text.Json;

namespace GuardianCapitalLLC.Services
{
    public static class JsonElementExtensions
    {
        public static decimal? GetDecimalOrNull(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.Number ? el.GetDecimal() : null;
        }
    }
    public class MarketDataService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _finnhubApiKey;

        private readonly string[] _targetCurrencies = new[] { "USD", "CAD", "EUR", "MXN", "GBP", "JPY", "KWD" };

        private static readonly Dictionary<string, string> FallbackCompanyNames = new()
        {
            { "QQQ", "Invesco QQQ Trust (NASDAQ 100 ETF)" },
            { "SPY", "SPDR S&P 500 ETF Trust" },
            { "DIA", "SPDR Dow Jones Industrial Average ETF" },
            { "IWM", "iShares Russell 2000 ETF" },
            { "USO", "United States Oil Fund" },
            { "GLD", "SPDR Gold Shares" },
            { "WEAT", "Teucrium Wheat Fund" }
        };

        public MarketDataService(
            IHttpClientFactory httpClientFactory,
            ILogger<HomeController> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _finnhubApiKey = _configuration["Finnhub:ApiKey"] ?? throw new InvalidOperationException("Finnhub API key is missing.");
        }

        public async Task<Dictionary<string, List<MarketQuoteVM>>> GetMarketDataAsync()
        {
            var categories = new Dictionary<string, List<string>>
        {
            { "Stocks", new List<string> { "AAPL", "MSFT", "TSLA" } },
            { "Indexes", new List<string> { "SPY", "QQQ", "DIA", "IWM" }  }, // S&P 500, NASDAQ, Dow Jones, Russell 2000
            { "Commodities", new List<string> { "USO", "GLD", "WEAT" } },    // Oil ETF, Gold ETF, Wheat ETF
            };


            var httpClient = _httpClientFactory.CreateClient();
            var marketData = new Dictionary<string, List<MarketQuoteVM>>();

            foreach (var category in categories)
            {
                var quotes = new List<MarketQuoteVM>();

                foreach (var symbol in category.Value)
                {
                    var quote = await FetchFinnhubQuoteAsync(httpClient, symbol);
                    if (quote != null)
                        quotes.Add(quote);
                }

                marketData[category.Key] = quotes;
            }

            return marketData;
        }

        private async Task<MarketQuoteVM?> FetchFinnhubQuoteAsync(HttpClient httpClient, string symbol)
        {
            var quoteUrl = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={_finnhubApiKey}";
            var profileUrl = $"https://finnhub.io/api/v1/stock/profile2?symbol={symbol}&token={_finnhubApiKey}";

            try
            {
                var quoteResponse = await httpClient.GetAsync(quoteUrl);
                if (!quoteResponse.IsSuccessStatusCode) return null;

                var quoteJson = await quoteResponse.Content.ReadAsStringAsync();
                var quoteData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(quoteJson);
                if (quoteData == null || !quoteData.ContainsKey("c")) return null;

                string? companyName = null;
                string? logoUrl = null;

                var profileResponse = await httpClient.GetAsync(profileUrl);
                if (profileResponse.IsSuccessStatusCode)
                {
                    var profileJson = await profileResponse.Content.ReadAsStringAsync();
                    var profileData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(profileJson);

                    if (profileData != null)
                    {
                        if (profileData.TryGetValue("name", out var nameElement))
                        {
                            companyName = nameElement.GetString();
                        }

                        if (string.IsNullOrWhiteSpace(companyName) && FallbackCompanyNames.TryGetValue(symbol, out var fallbackName))
                        {
                            companyName = fallbackName;
                        }

                        if (profileData.TryGetValue("logo", out var logoElement))
                        {
                            logoUrl = logoElement.GetString();
                        }
                    }
                }

                return new MarketQuoteVM
                {
                    Symbol = symbol,
                    Current = quoteData["c"].GetDecimalOrNull(),
                    High = quoteData["h"].GetDecimalOrNull(),
                    Low = quoteData["l"].GetDecimalOrNull(),
                    Open = quoteData["o"].GetDecimalOrNull(),
                    PreviousClose = quoteData["pc"].GetDecimalOrNull(),
                    CompanyName = companyName,
                    LogoUrl = logoUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Finnhub error for symbol: {Symbol}", symbol);
                return null;
            }
        }

        public async Task<Dictionary<string, decimal>> GetConvertedBalancesAsync(decimal totalBalanceUsd)
        {
            var convertedBalances = new Dictionary<string, decimal>();

            string url = "https://api.exchangerate-api.com/v4/latest/USD";

            ExchangeRatesResponse? ratesResponse = null;

            try
            {
                ratesResponse = await _httpClient.GetFromJsonAsync<ExchangeRatesResponse>(url);
            }
            catch
            {
                // Optionally handle error/logging or fallback logic here
                return convertedBalances;
            }

            if (ratesResponse != null)
            {
                foreach (var currency in _targetCurrencies)
                {
                    if (currency == "USD")
                    {
                        convertedBalances["USD"] = Math.Round(totalBalanceUsd, 2);
                    }
                    else if (ratesResponse.Rates.TryGetValue(currency, out var rate))
                    {
                        convertedBalances[currency] = Math.Round(totalBalanceUsd * rate, 2);
                    }
                }
            }

            return convertedBalances;
        }

    }

}
