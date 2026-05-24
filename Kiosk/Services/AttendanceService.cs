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
        public Task LogAsync(
            long studentId,
            long courseId,
            long classroomId,
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
