using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
            SupabaseRestClient.Configure(_http);
        }

         public async Task<(bool success, string? role, long? userId, string? name)> LoginAsync(string email, string password)
        {
            // --- HARDCODED ADMIN CHECK ---
            if (email == "admin@facepass.com" && password == "admin123")
            {
                return (true, "admin", 0L, "System Admin");
            }
            // -----------------------------

            try
            {
                var url = $"{_baseUrl}/rest/v1/USER?email=eq.{email}&select=user_id,first_name,last_name,password_hash,role_id,ROLE(role_name)";
                var resp = await _http.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var arr = JArray.Parse(json);

                if (arr.Count == 0) return (false, null, null, null);

                var user = arr[0];
                string storedHash = user["password_hash"]?.ToString() ?? "";

                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, storedHash);

                if (isPasswordCorrect)
                {
                    var first = user["first_name"]?.ToString() ?? "";
                    var last = user["last_name"]?.ToString() ?? "";
                    var fullName = $"{first} {last}".Trim();

                    return (true,
                            JsonEmbedHelper.RoleNameFromUser(user),
                            long.Parse(user["user_id"]!.ToString()),
                            fullName);
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
