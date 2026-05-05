using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FacePass.Kiosk.Services
{
    /// <summary>
    /// Low-level Supabase REST API client shared across all repositories.
    /// Injects the API key into every request header.
    /// </summary>
    public class SupabaseService : IDisposable
    {
        private readonly HttpClient _http;
        public string BaseUrl { get; }

        public SupabaseService(string baseUrl, string anonKey)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(10);
            _http.DefaultRequestHeaders.Add("apikey", anonKey);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", anonKey);
            _http.DefaultRequestHeaders.Accept
                 .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<JArray> GetAsync(string path)
        {
            var resp = await _http.GetAsync($"{BaseUrl}{path}");
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            return JArray.Parse(body);
        }

        public async Task<JObject?> PostAsync(string path, JObject payload)
        {
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            // Prefer: return representation
            var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}")
            {
                Content = content
            };
            req.Headers.Add("Prefer", "return=representation");

            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body) || body == "[]") return null;
            var arr = JArray.Parse(body);
            return arr.Count > 0 ? (JObject)arr[0] : null;
        }

        public async Task PatchAsync(string path, JObject payload)
        {
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}{path}")
            {
                Content = content
            };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        public void Dispose() => _http?.Dispose();
    }
}
