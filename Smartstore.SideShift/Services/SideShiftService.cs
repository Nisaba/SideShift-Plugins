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

        public async Task<string> CreateCheckout(SideShiftRequest request, string apiSecret, string ip)
        {
            string sRep = "";
            try
            {
                request.affiliateId = Referal;

                using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
                client.DefaultRequestHeaders.Add("x-sideshift-secret", apiSecret);
                client.DefaultRequestHeaders.Add("x-sideshift-ip", ip);
                var s = JsonSerializer.Serialize(request);
                using var response = await client.PostAsync(
                    "checkout",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
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
                throw new Exception(sMsg);
            }
        }

        public async Task<bool> InitWebHook(string urlWebHook, string ApiSecret)
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

            return doc.RootElement
                .GetProperty("data")
                .GetProperty("createHook")
                .GetProperty("enabled")
                .GetBoolean();
        }
    }
}
