using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FacePass.Kiosk.Services
{
    /// <summary>
    /// Repository for student face encodings and attendance logging via Supabase REST.
    /// </summary>
    public class SupabaseFaceRepository
    {
        private readonly SupabaseService _supa;

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

        public SupabaseFaceRepository(SupabaseService supa) => _supa = supa;

        /// <summary>
        /// Fetches all student IDs and their BYTEA face encodings from Supabase.
        /// The vector_data_bytea column is returned as Base64 by Supabase REST.
        /// </summary>
        public async Task<Dictionary<long, byte[]>> GetAllEncodingsAsync()
        {
            var rows = await _supa.GetAsync("/rest/v1/STUDENTS?select=student_id,FACE_ENCODINGS(vector_data_bytea)");
            var dict = new Dictionary<long, byte[]>();

            foreach (var item in rows)
            {
                var studentId = long.Parse(item["student_id"]!.ToString());
                foreach (var bytes in ExtractEncodingBytes(item["FACE_ENCODINGS"]))
                    dict[studentId] = bytes;
            }

            return dict;
        }

        private static IEnumerable<byte[]> ExtractEncodingBytes(JToken? encodingsToken)
        {
            if (encodingsToken == null) yield break;

            if (encodingsToken is JArray arr)
            {
                foreach (var entry in arr)
                {
                    var b64 = entry["vector_data_bytea"]?.ToString();
                    if (!string.IsNullOrEmpty(b64))
                        yield return Convert.FromBase64String(b64);
                }
            }
            else if (encodingsToken is JObject obj)
            {
                var b64 = obj["vector_data_bytea"]?.ToString();
                if (!string.IsNullOrEmpty(b64))
                    yield return Convert.FromBase64String(b64);
            }
        }

        /// <summary>
        /// Retrieves the display name for a student by joining the USER table.
        /// </summary>
        public async Task<string> GetStudentNameAsync(long studentId)
        {
            var rows = await _supa.GetAsync(
                $"/rest/v1/STUDENTS?student_id=eq.{studentId}&select=USER(first_name,last_name)");

            if (rows.Count == 0) return "Student";
            var user = rows[0]["USER"] as JObject;
            var first = user?["first_name"]?.ToString() ?? "";
            var last = user?["last_name"]?.ToString() ?? "";
            var name = $"{first} {last}".Trim();
            return string.IsNullOrEmpty(name) ? "Student" : name;
        }

        /// <summary>
        /// Inserts a new row into attendance_logs.
        /// </summary>
        public async Task InsertAttendanceAsync(
            long studentId,
            long courseId,
            long classroomId,
            string method,
            string status,
            string? flaggedReason = null)
        {
            if (!MethodMap.TryGetValue(method, out var methodId))
                throw new ArgumentException($"Unknown attendance method: {method}", nameof(method));
            if (!StatusMap.TryGetValue(status, out var statusId))
                throw new ArgumentException($"Unknown attendance status: {status}", nameof(status));

            var payload = new JObject
            {
                ["student_id"]   = studentId,
                ["course_id"]    = courseId,
                ["classroom_id"] = classroomId,
                ["method_id"]    = methodId,
                ["status_id"]    = statusId
            };

            if (!string.IsNullOrEmpty(flaggedReason))
                payload["flagged_reason"] = flaggedReason;

            await _supa.PostAsync("/rest/v1/attendance_logs", payload);
        }

        /// <summary>
        /// Inserts a new QR session row. Supabase auto-generates session_id and session_guid.
        /// </summary>
        public async Task<JObject?> InsertQrSessionAsync(long classroomId, DateTime expiresAt)
        {
            var payload = new JObject
            {
                ["classroom_id"] = classroomId,
                ["expires_at"]   = expiresAt.ToUniversalTime().ToString("o")
            };
            return await _supa.PostAsync("/rest/v1/qr_sessions", payload);
        }
    }
}
