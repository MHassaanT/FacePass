using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using BCrypt.Net;

namespace FacePass.Management.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _anonKey;

        public AuthService(string baseUrl, string anonKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _anonKey = anonKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", _anonKey);
        }

         public async Task<(bool success, string? role, Guid? userId, string? name)> LoginAsync(string email, string password)
        {
            // --- HARDCODED ADMIN CHECK ---
            if (email == "admin@facepass.com" && password == "admin123")
            {
                return (true, "admin", Guid.Empty, "System Admin");
            }
            // -----------------------------

            try
            {
                // 1. Fetch user data by email
                var url = $"{_baseUrl}/rest/v1/users?email=eq.{email}&select=id,name,password_hash,role";
                var resp = await _http.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var arr = JArray.Parse(json);

                if (arr.Count == 0) return (false, null, null, null);

                var user = arr[0];
                string storedHash = user["password_hash"]?.ToString() ?? "";

                // 2. Verify BCrypt Hash
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, storedHash);

                if (isPasswordCorrect)
                {
                    return (true, 
                            user["role"]?.ToString(), 
                            Guid.Parse(user["id"]!.ToString()), 
                            user["name"]?.ToString());
                }

                return (false, null, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Auth] Login Error: {ex.Message}");
                return (false, null, null, null);
            }
        }
    }
}
