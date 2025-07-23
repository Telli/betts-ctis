using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public interface ICurrencyExchangeService
    {
        Task<Result<decimal>> GetExchangeRateAsync(string fromCurrency, string toCurrency);
        Task<Result<decimal>> ConvertAmountAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<Result<Dictionary<string, decimal>>> GetSupportedCurrenciesAsync();
        Task<Result<ExchangeRateInfo>> GetExchangeRateInfoAsync(string fromCurrency, string toCurrency);
    }

    public class ExchangeRateInfo
    {
        public string FromCurrency { get; set; } = "";
        public string ToCurrency { get; set; } = "";
        public decimal Rate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Source { get; set; } = "";
        public decimal ConvertedAmount { get; set; }
        public decimal OriginalAmount { get; set; }
    }

    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyExchangeService> _logger;
        private readonly Dictionary<string, decimal> _fallbackRates;
        private readonly Dictionary<string, (decimal rate, DateTime lastUpdated)> _rateCache;

        public CurrencyExchangeService(HttpClient httpClient, ILogger<CurrencyExchangeService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _rateCache = new Dictionary<string, (decimal, DateTime)>();
            
            // Fallback exchange rates (should be updated regularly)
            _fallbackRates = new Dictionary<string, decimal>
            {
                ["SLE_USD"] = 0.000045m, // 1 SLE = 0.000045 USD (approximate)
                ["USD_SLE"] = 22000m,    // 1 USD = 22,000 SLE (approximate)
                ["EUR_USD"] = 1.08m,     // 1 EUR = 1.08 USD
                ["GBP_USD"] = 1.26m,     // 1 GBP = 1.26 USD
                ["CAD_USD"] = 0.74m,     // 1 CAD = 0.74 USD
                ["AUD_USD"] = 0.66m      // 1 AUD = 0.66 USD
            };
        }

        public async Task<Result<decimal>> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                if (fromCurrency == toCurrency)
                    return Result.Success(1.0m);

                var cacheKey = $"{fromCurrency}_{toCurrency}";
                
                // Check cache first (refresh every hour)
                if (_rateCache.TryGetValue(cacheKey, out var cached) && 
                    (DateTime.UtcNow - cached.lastUpdated).TotalHours < 1)
                {
                    return Result.Success(cached.rate);
                }

                // Try to get live rate from exchange rate API
                var liveRate = await GetLiveExchangeRateAsync(fromCurrency, toCurrency);
                if (liveRate.IsSuccess)
                {
                    _rateCache[cacheKey] = (liveRate.Value, DateTime.UtcNow);
                    return liveRate;
                }

                // Fall back to static rates
                if (_fallbackRates.TryGetValue(cacheKey, out var fallbackRate))
                {
                    _logger.LogWarning("Using fallback exchange rate for {From} to {To}: {Rate}",
                        fromCurrency, toCurrency, fallbackRate);
                    return Result.Success(fallbackRate);
                }

                // Try inverse rate
                var inverseKey = $"{toCurrency}_{fromCurrency}";
                if (_fallbackRates.TryGetValue(inverseKey, out var inverseRate))
                {
                    var rate = 1 / inverseRate;
                    _logger.LogWarning("Using inverse fallback exchange rate for {From} to {To}: {Rate}",
                        fromCurrency, toCurrency, rate);
                    return Result.Success(rate);
                }

                return Result.Failure<decimal>($"Exchange rate not available for {fromCurrency} to {toCurrency}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate from {From} to {To}", fromCurrency, toCurrency);
                return Result.Failure<decimal>("Failed to get exchange rate");
            }
        }

        public async Task<Result<decimal>> ConvertAmountAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            try
            {
                var rateResult = await GetExchangeRateAsync(fromCurrency, toCurrency);
                if (!rateResult.IsSuccess)
                    return Result.Failure<decimal>(rateResult.ErrorMessage);

                var convertedAmount = amount * rateResult.Value;
                return Result.Success(convertedAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting amount {Amount} from {From} to {To}", 
                    amount, fromCurrency, toCurrency);
                return Result.Failure<decimal>("Failed to convert amount");
            }
        }

        public async Task<Result<ExchangeRateInfo>> GetExchangeRateInfoAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                var rateResult = await GetExchangeRateAsync(fromCurrency, toCurrency);
                if (!rateResult.IsSuccess)
                    return Result.Failure<ExchangeRateInfo>(rateResult.ErrorMessage);

                var cacheKey = $"{fromCurrency}_{toCurrency}";
                var lastUpdated = _rateCache.TryGetValue(cacheKey, out var cached) ? 
                    cached.lastUpdated : DateTime.UtcNow;

                return Result.Success(new ExchangeRateInfo
                {
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    Rate = rateResult.Value,
                    LastUpdated = lastUpdated,
                    Source = _rateCache.ContainsKey(cacheKey) ? "Live API" : "Fallback"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate info");
                return Result.Failure<ExchangeRateInfo>("Failed to get exchange rate info");
            }
        }

        public async Task<Result<Dictionary<string, decimal>>> GetSupportedCurrenciesAsync()
        {
            try
            {
                var supportedRates = new Dictionary<string, decimal>();
                
                // Add all currencies we have rates for relative to USD
                var baseCurrencies = new[] { "SLE", "USD", "EUR", "GBP", "CAD", "AUD" };
                
                foreach (var currency in baseCurrencies)
                {
                    if (currency == "USD")
                    {
                        supportedRates[currency] = 1.0m;
                        continue;
                    }

                    var rateResult = await GetExchangeRateAsync(currency, "USD");
                    if (rateResult.IsSuccess)
                    {
                        supportedRates[currency] = rateResult.Value;
                    }
                }

                return Result.Success(supportedRates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported currencies");
                return Result.Failure<Dictionary<string, decimal>>("Failed to get supported currencies");
            }
        }

        private async Task<Result<decimal>> GetLiveExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                // Try multiple exchange rate APIs in order of preference
                
                // 1. Try exchangerate-api.com (free tier)
                var exchangeRateApiResult = await TryExchangeRateApiAsync(fromCurrency, toCurrency);
                if (exchangeRateApiResult.IsSuccess)
                    return exchangeRateApiResult;

                // 2. Try fixer.io (if API key available)
                var fixerResult = await TryFixerIoAsync(fromCurrency, toCurrency);
                if (fixerResult.IsSuccess)
                    return fixerResult;

                // 3. Try Central Bank APIs for SLE rates
                if (fromCurrency == "SLE" || toCurrency == "SLE")
                {
                    var centralBankResult = await TrySierraLeoneCentralBankAsync(fromCurrency, toCurrency);
                    if (centralBankResult.IsSuccess)
                        return centralBankResult;
                }

                return Result.Failure<decimal>("No live exchange rate sources available");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live exchange rate");
                return Result.Failure<decimal>("Failed to get live exchange rate");
            }
        }

        private async Task<Result<decimal>> TryExchangeRateApiAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                // Free API: https://api.exchangerate-api.com/v4/latest/{base_currency}
                var response = await _httpClient.GetAsync($"https://api.exchangerate-api.com/v4/latest/{fromCurrency}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var rateData = JsonSerializer.Deserialize<ExchangeRateApiResponse>(content);
                    
                    if (rateData?.Rates?.TryGetValue(toCurrency, out var rate) == true)
                    {
                        _logger.LogDebug("Got live exchange rate from exchangerate-api.com: {From} to {To} = {Rate}",
                            fromCurrency, toCurrency, rate);
                        return Result.Success(rate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "exchangerate-api.com failed for {From} to {To}", fromCurrency, toCurrency);
            }

            return Result.Failure<decimal>("exchangerate-api.com unavailable");
        }

        private async Task<Result<decimal>> TryFixerIoAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                // Requires API key: https://api.fixer.io/latest?access_key=YOUR_KEY&base={from}&symbols={to}
                // This would need configuration for the API key
                return Result.Failure<decimal>("Fixer.io not configured");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Fixer.io failed for {From} to {To}", fromCurrency, toCurrency);
                return Result.Failure<decimal>("Fixer.io unavailable");
            }
        }

        private async Task<Result<decimal>> TrySierraLeoneCentralBankAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                // Sierra Leone Central Bank might have API endpoints for official rates
                // This would need to be implemented based on their actual API
                return Result.Failure<decimal>("Sierra Leone Central Bank API not implemented");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Sierra Leone Central Bank API failed");
                return Result.Failure<decimal>("Central Bank API unavailable");
            }
        }
    }

    // API response models
    internal class ExchangeRateApiResponse
    {
        public string Base { get; set; } = "";
        public string Date { get; set; } = "";
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}