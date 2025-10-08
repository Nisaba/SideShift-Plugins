using System.Text;
using System.Text.Json;
using Smartstore.Core.Checkout.Payment;
using Smartstore.SideShift.Models;
using Smartstore.Utilities;

namespace Smartstore.SideShift.Services
{
    public class SideShiftService
    {
        private const string BaseUrl = "https://sideshift.ai/api/v2/";
        private const string Referal = "Hj0WWsdiX";

        public static async Task<string> CreateCheckout(SideShiftRequest request, string apiSecret, string ip)
        {
            string sRep = "";
            request.affiliateId = Referal;
            string sJson = JsonSerializer.Serialize(request);
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
                client.DefaultRequestHeaders.Add("x-sideshift-secret", apiSecret);
                client.DefaultRequestHeaders.Add("x-sideshift-ip", ip);

                using var response = await client.PostAsync(
                    "checkout",
                    new StringContent(sJson, Encoding.UTF8, "application/json")
                );
                sRep = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(sRep);
                return doc.RootElement.GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                var sMsg = ex.Message;
                if (!string.IsNullOrEmpty(sRep))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(sRep);
                        if (doc.RootElement.TryGetProperty("error", out var error))
                        {
                            if (error.TryGetProperty("message", out var message))
                            {
                                sMsg += ": " + message.GetString() ?? "";
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                sMsg += "\r\n" + sJson;
                throw new Exception(sMsg);
            }
        }

        public static async Task<ushort> CheckCoin(string coin, string network, string memo)
        {
            using var client = new HttpClient();
            using var doc = JsonDocument.Parse(await client.GetStringAsync(BaseUrl + "coins"));

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("coin", out var coinProp) || !string.Equals(coinProp.GetString(), coin, StringComparison.OrdinalIgnoreCase))
                    continue;

                var networks = item.GetProperty("networks");
                bool foundNetwork = false;
                foreach (var net in networks.EnumerateArray())
                    if (string.Equals(net.GetString(), network, StringComparison.OrdinalIgnoreCase))
                    { foundNetwork = true; break; }

                if (!foundNetwork) throw new Exception($"Network '{network}' not found for coin '{coin}'");

                if (item.GetProperty("hasMemo").GetBoolean() && string.IsNullOrEmpty(memo))
                    throw new Exception($"Coin '{coin}' on network '{network}' requires a memo/tag");

                if (item.TryGetProperty("tokenDetails", out var tokenDetails)
                    && tokenDetails.TryGetProperty(network, out var networkDetails)
                    && networkDetails.TryGetProperty("decimals", out var decimals))
                {
                    return (ushort)decimals.GetInt32();
                }

                return 8;
            }

            throw new Exception($"Coin '{coin}' not found");
        }
        public static async Task<Tuple<bool, string?>> InitWebHook(string urlWebHook, string ApiSecret)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-sideshift-secret", ApiSecret);

            var query = new
            {
                query = $@"mutation {{
                    createHook(targetUrl: ""{urlWebHook}"") {{
                        id
                        createdAt
                        updatedAt
                        targetUrl
                        enabled
                    }}
                }}"
            };

            var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");

            using var response = await client.PostAsync("https://sideshift.ai/graphql", content);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                var firstError = errors[0];
                if (firstError.TryGetProperty("message", out var message))
                {
                    throw new Exception (message.GetString() ?? "");
                }
                throw new Exception();
            }

            var bFlag =  doc.RootElement
                .GetProperty("data")
                .GetProperty("createHook")
                .GetProperty("enabled")
                .GetBoolean();

            var sId = string.Empty;
            if (bFlag)
            {
                sId = doc.RootElement
                .GetProperty("data")
                .GetProperty("createHook")
                .GetProperty("id")
                .GetString();
            }
            return new Tuple<bool, string?>(bFlag, sId);
        }

        public static async Task DeleteWebHook(string hookId, string ApiSecret)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-sideshift-secret", ApiSecret);
            var query = new
            {
                query = $@"mutation {{
                    deleteHook(id: ""{hookId}"")
                }}"
            };
            var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("https://sideshift.ai/graphql", content);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                var firstError = errors[0];
                if (firstError.TryGetProperty("message", out var message))
                {
                    throw new Exception(message.GetString() ?? "");
                }
                throw new Exception();
            }
            var bFlag = doc.RootElement
                .GetProperty("data")
                .GetProperty("deleteHook")
                .GetBoolean();
            if (!bFlag)
            {
                throw new Exception("Failed to delete webhook");
            }
        }
    }
}
