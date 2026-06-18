using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace DeepSeekCreditCheck.Core.Services
{
    /// <summary>
    /// Klientská třída pro přístup k internímu API platformy DeepSeek (platform.deepseek.com).
    /// Vyžaduje uživatelský session token (Bearer token) zkopírovaný z prohlížeče nebo zachycený pomocí WebView2.
    /// </summary>
    public class DeepSeekPlatformClient : IDeepSeekPlatformClient
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeClient;
        private const string BaseUrl = "https://platform.deepseek.com";

        public DeepSeekPlatformClient()
        {
            _httpClient = new HttpClient();
            _disposeClient = true;
            
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public DeepSeekPlatformClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _disposeClient = false;
            
            // Nastavení standardního User-Agent, aby request vypadal jako z běžného prohlížeče, pokud ještě není nastaven
            if (!_httpClient.DefaultRequestHeaders.UserAgent.Contains(new ProductInfoHeaderValue("Mozilla", "5.0")))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            }
            if (!_httpClient.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json")))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        /// <summary>
        /// Provede GET dotaz na specifikovanou cestu interního API s předaným session tokenem.
        /// Vrátí surový JSON jako JsonNode pro maximální flexibilitu.
        /// </summary>
        public async Task<JsonNode?> GetJsonAsync(string path, string sessionToken, Dictionary<string, string>? queryParams = null)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new ArgumentException("Session token nesmí být prázdný.", nameof(sessionToken));
            }

            // Normalizace cesty (odstranění úvodního lomítka, pokud existuje)
            var cleanPath = path.StartsWith("/") ? path.Substring(1) : path;
            var uriBuilder = new UriBuilder($"{BaseUrl}/{cleanPath}");

            // Přidání query parametrů
            if (queryParams != null && queryParams.Count > 0)
            {
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                foreach (var param in queryParams)
                {
                    query[param.Key] = param.Value;
                }
                uriBuilder.Query = query.ToString();
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            
            // Nastavení autorizační hlavičky s Bearer tokenem
            var tokenValue = sessionToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                ? sessionToken.Substring(7) 
                : sessionToken;
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue.Trim());

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Chyba při volání API ({response.StatusCode}): {errorContent}");
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonNode.ParseAsync(contentStream);
        }

        /// <summary>
        /// Získá uživatelský přehled (zůstatek, základní údaje).
        /// Volá: /api/v0/users/get_user_summary
        /// </summary>
        public async Task<JsonNode?> GetUserSummaryAsync(string sessionToken)
        {
            return await GetJsonAsync("/api/v0/users/get_user_summary", sessionToken);
        }

        /// <summary>
        /// Získá množství spotřebovaných tokenů za definovaný rok a měsíc.
        /// Volá: /api/v0/usage/amount
        /// </summary>
        public async Task<JsonNode?> GetUsageAmountAsync(string sessionToken, int year, int month)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "year", year.ToString() },
                { "month", month.ToString() }
            };

            return await GetJsonAsync("/api/v0/usage/amount", sessionToken, queryParams);
        }

        /// <summary>
        /// Získá finanční náklady na spotřebu za definovaný rok a měsíc.
        /// Volá: /api/v0/usage/cost
        /// </summary>
        public async Task<JsonNode?> GetUsageCostAsync(string sessionToken, int year, int month)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "year", year.ToString() },
                { "month", month.ToString() }
            };

            return await GetJsonAsync("/api/v0/usage/cost", sessionToken, queryParams);
        }

        /// <summary>
        /// Stáhne ZIP s podrobnými CSV výkazy spotřeby a nákladů.
        /// Volá: GET /api/v0/usage/export?month=X&year=Y
        /// </summary>
        public async Task<byte[]> GetUsageExportZipAsync(string sessionToken, int year, int month)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new ArgumentException("Session token nesmí být prázdný.", nameof(sessionToken));
            }
 
            var uriBuilder = new UriBuilder($"{BaseUrl}/api/v0/usage/export");
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["year"] = year.ToString();
            query["month"] = month.ToString();
            uriBuilder.Query = query.ToString();
 
            using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            var tokenValue = sessionToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
                ? sessionToken.Substring(7) 
                : sessionToken;
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue.Trim());
 
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Chyba při volání export API ({response.StatusCode}): {errorContent}");
            }
 
            return await response.Content.ReadAsByteArrayAsync();
        }

        public void Dispose()
        {
            if (_disposeClient)
            {
                _httpClient.Dispose();
            }
        }
    }
}
