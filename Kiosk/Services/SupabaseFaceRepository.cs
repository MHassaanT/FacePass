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

        public SupabaseFaceRepository(SupabaseService supa) => _supa = supa;

        /// <summary>
        /// Fetches all student IDs and their BYTEA face encodings from Supabase.
        /// The face_encoding column is returned as Base64 by Supabase REST.
        /// </summary>
        public async Task<Dictionary<Guid, byte[]>> GetAllEncodingsAsync()
        {
            var rows = await _supa.GetAsync("/rest/v1/students?select=id,face_encoding");
            var dict = new Dictionary<Guid, byte[]>();

            foreach (var item in rows)
            {
                if (item["face_encoding"] == null) continue;
                var id = Guid.Parse(item["id"]!.ToString());
                var encBase64 = item["face_encoding"]!.ToString();
                var encBytes = Convert.FromBase64String(encBase64);
                dict[id] = encBytes;
            }

            return dict;
        }

        /// <summary>
        /// Retrieves the display name for a student by joining the users table.
        /// </summary>
        public async Task<string> GetStudentNameAsync(Guid studentId)
        {
            var rows = await _supa.GetAsync(
                $"/rest/v1/students?id=eq.{studentId}&select=users(name)");

            if (rows.Count == 0) return "Student";
            return rows[0]["users"]?["name"]?.ToString() ?? "Student";
        }

        /// <summary>
        /// Inserts a new row into attendance_logs.
        /// </summary>
        public async Task InsertAttendanceAsync(
            Guid studentId,
            Guid courseId,
            Guid classroomId,
            string method,
            string status,
            string? flaggedReason = null)
        {
            var payload = new JObject
            {
                ["student_id"]   = studentId,
                ["course_id"]    = courseId,
                ["classroom_id"] = classroomId,
                ["method"]       = method,
                ["status"]       = status
            };

            if (!string.IsNullOrEmpty(flaggedReason))
                payload["flagged_reason"] = flaggedReason;

            await _supa.PostAsync("/rest/v1/attendance_logs", payload);
        }

        /// <summary>
        /// Inserts a new QR session row. Supabase auto-generates id and session_guid.
        /// </summary>
        public async Task<JObject?> InsertQrSessionAsync(Guid classroomId, DateTime expiresAt)
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
