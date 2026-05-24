using System.Net.Http;
using System.Net.Http.Headers;

namespace FacePass.Management.Services
{
    /// <summary>
    /// Shared Supabase REST configuration. PostgREST requires both apikey and Authorization: Bearer.
    /// </summary>
    public static class SupabaseRestClient
    {
        public const string BaseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        public const string AnonKey =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";

        public static HttpClient Create()
        {
            var client = new HttpClient();
            Configure(client);
            return client;
        }

        public static void Configure(HttpClient client)
        {
            if (!client.DefaultRequestHeaders.Contains("apikey"))
                client.DefaultRequestHeaders.Add("apikey", AnonKey);

            client.DefaultRequestHeaders.Authorization ??=
                new AuthenticationHeaderValue("Bearer", AnonKey);
        }
    }
}
