using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FacePass.Mobile.Services
{
    public class SupabaseMobileService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _anonKey;

        public SupabaseMobileService(string baseUrl, string anonKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _anonKey = anonKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", _anonKey);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);
        }

        public async Task<JArray> GetAttendanceHistory(Guid studentId)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs?student_id=eq.{studentId}&select=*,courses(name),classrooms(name)&order=timestamp.desc";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return JArray.Parse(await resp.Content.ReadAsStringAsync());
        }

        public async Task<JObject?> GetStudentStats(Guid studentId)
        {
            // Fetch total sessions and present sessions for stats
            var url = $"{_baseUrl}/rest/v1/attendance_logs?student_id=eq.{studentId}&select=status";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var logs = JArray.Parse(await resp.Content.ReadAsStringAsync());

            int total = logs.Count;
            int present = 0;
            foreach (var log in logs)
            {
                if (log["status"]?.ToString() == "present") present++;
            }

            return new JObject
            {
                ["total_sessions"] = total,
                ["present_count"] = present,
                ["attendance_percentage"] = total > 0 ? (double)present / total * 100 : 0
            };
        }

        public async Task LogAttendance(Guid studentId, Guid courseId, Guid classroomId, string method, string status)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs";
            var payload = new JObject
            {
                ["student_id"] = studentId,
                ["course_id"] = courseId,
                ["classroom_id"] = classroomId,
                ["method"] = method,
                ["status"] = status
            };

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();
        }

        public async Task SubmitDispute(Guid attendanceId, string reason)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs?id=eq.{attendanceId}";
            var payload = new JObject
            {
                ["flagged_reason"] = reason,
                ["status"] = "suspicious" // Ensure it remains suspicious but with a reason
            };

            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
            };

            var resp = await _http.SendAsync(request);
            resp.EnsureSuccessStatusCode();
        }
    }
}
