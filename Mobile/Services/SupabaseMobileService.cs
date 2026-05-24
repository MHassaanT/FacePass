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

        private static readonly Dictionary<string, int> MethodMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["face"] = 1,
            ["qr"] = 2,
            ["manual"] = 3,
            ["gps_auto"] = 4
        };

        private static readonly Dictionary<string, int> StatusMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["present"] = 1,
            ["suspicious"] = 2,
            ["manual_override"] = 3,
            ["absent"] = 4
        };

        public SupabaseMobileService(string baseUrl, string anonKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _anonKey = anonKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("apikey", _anonKey);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);
        }

        public async Task<JArray> GetAttendanceHistory(long studentId)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs?student_id=eq.{studentId}&select=*,COURSES(course_name),CLASSROOMS(room_number)&order=timestamp.desc";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return JArray.Parse(await resp.Content.ReadAsStringAsync());
        }

        public async Task<JObject?> GetStudentStats(long studentId)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs?student_id=eq.{studentId}&select=status_id";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var logs = JArray.Parse(await resp.Content.ReadAsStringAsync());

            int total = logs.Count;
            int present = 0;
            foreach (var log in logs)
            {
                if (log["status_id"]?.Value<int>() == 1) present++;
            }

            return new JObject
            {
                ["total_sessions"] = total,
                ["present_count"] = present,
                ["attendance_percentage"] = total > 0 ? (double)present / total * 100 : 0
            };
        }

        public async Task LogAttendance(long studentId, long courseId, long classroomId, string method, string status)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs";
            var payload = new JObject
            {
                ["student_id"] = studentId,
                ["course_id"] = courseId,
                ["classroom_id"] = classroomId,
                ["method_id"] = MethodMap[method],
                ["status_id"] = StatusMap[status]
            };

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();
        }

        public async Task SubmitDispute(long attendanceId, string reason)
        {
            var url = $"{_baseUrl}/rest/v1/attendance_logs?log_id=eq.{attendanceId}";
            var payload = new JObject
            {
                ["flagged_reason"] = reason,
                ["status_id"] = 2
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
