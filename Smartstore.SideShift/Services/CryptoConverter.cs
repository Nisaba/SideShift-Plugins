using System.Text.Json;

public class CryptoConverter
{
    public static async Task<decimal> GetCryptoAmountAsync(decimal fiatAmount, string fiatCurrency, string cryptoCurrency)
    {
        HttpClient _httpClient = new HttpClient();
        if (fiatCurrency == "USD" && cryptoCurrency.Contains("USD", StringComparison.OrdinalIgnoreCase))
        {
            return fiatAmount;
        }

        string url = $"https://min-api.cryptocompare.com/data/price?fsym={fiatCurrency}&tsyms={cryptoCurrency}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error fetching exchange rate from CryptoCompare API");
        }

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty(cryptoCurrency, out var value))
        {
            decimal rate = value.GetDecimal();
            return fiatAmount * rate;
        }

        throw new Exception("Crypto currency not found in API response");
    }
}
