using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;

namespace FacePass.Shared.Services
{
    public class StorageService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _anonKey;

        public StorageService(string baseUrl, string anonKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _anonKey = anonKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", _anonKey);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);
        }

        public async Task<string?> UploadAvatarAsync(Guid userId, byte[] imageBytes)
        {
            try
            {
                var filePath = $"{userId}/avatar.jpg";
                var url = $"{_baseUrl}/storage/v1/object/avatars/{filePath}";

                var content = new ByteArrayContent(imageBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                var resp = await _http.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Storage] Upload Error: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetSignedUrlAsync(string path)
        {
            try
            {
                var url = $"{_baseUrl}/storage/v1/object/sign/avatars/{path}";
                var payload = new JObject { ["expiresIn"] = 3600 };
                
                var content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var result = JObject.Parse(json);
                
                return $"{_baseUrl}{result["signedURL"]}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Storage] SignedUrl Error: {ex.Message}");
                return null;
            }
        }
    }
}
