using System.Text.Json;

public class CryptoConverter
{
    public static async Task<decimal> GetCorrespondingAmountAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        HttpClient _httpClient = new HttpClient();
        if (fromCurrency.Contains("USD", StringComparison.OrdinalIgnoreCase) && toCurrency.Contains("USD", StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        string url = $"https://min-api.cryptocompare.com/data/price?fsym={fromCurrency}&tsyms={toCurrency}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error fetching exchange rate from CryptoCompare API");
        }

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty(toCurrency, out var value))
        {
            decimal rate = value.GetDecimal();
            return amount * rate;
        }

        throw new Exception("To currency not found in API response");
    }
}
