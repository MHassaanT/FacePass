using System;
using System.Threading.Tasks;

namespace FacePass.Kiosk.Services
{
    /// <summary>
    /// Thin service wrapping SupabaseFaceRepository.InsertAttendanceAsync.
    /// Provides a clean interface for the ViewModel to log attendance events.
    /// </summary>
    public class AttendanceService
    {
        private readonly SupabaseFaceRepository _repo;

        public AttendanceService(SupabaseFaceRepository repo) => _repo = repo;

        /// <summary>
        /// Logs an attendance event to the attendance_logs table in Supabase.
        /// </summary>
        /// <param name="studentId">The matched student's UUID.</param>
        /// <param name="courseId">Active course UUID.</param>
        /// <param name="classroomId">Classroom UUID for this kiosk.</param>
        /// <param name="method">Recognition method: "face", "qr", or "manual".</param>
        /// <param name="status">"present", "suspicious", or "manual_override".</param>
        /// <param name="flaggedReason">Optional reason if status is suspicious.</param>
        public Task LogAsync(
            Guid studentId,
            Guid courseId,
            Guid classroomId,
            string method,
            string status,
            string? flaggedReason = null)
        {
            return _repo.InsertAttendanceAsync(
                studentId,
                courseId,
                classroomId,
                method,
                status,
                flaggedReason);
        }
    }
}
