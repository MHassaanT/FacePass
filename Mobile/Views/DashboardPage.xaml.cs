using FacePass.Mobile.Services;
using System.Collections.ObjectModel;

namespace FacePass.Mobile.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly SupabaseMobileService _supabase;
        public ObservableCollection<object> RecentHistory { get; set; } = new();

        public DashboardPage(SupabaseMobileService supabase)
        {
            InitializeComponent();
            _supabase = supabase;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                // Placeholder Student ID
                Guid studentId = Guid.NewGuid();

                var stats = await _supabase.GetStudentStats(studentId);
                if (stats != null)
                {
                    AttendancePercentageLabel.Text = $"{(double)stats["attendance_percentage"]:F0}%";
                    AttendanceProgressBar.Progress = (double)stats["attendance_percentage"] / 100;
                    TotalClassesLabel.Text = stats["total_sessions"]?.ToString();
                    AttendedLabel.Text = stats["present_count"]?.ToString();
                }

                var history = await _supabase.GetAttendanceHistory(studentId);
                RecentHistory.Clear();
                foreach (var item in history)
                {
                    RecentHistory.Add(new
                    {
                        course_name = item["courses"]?["name"]?.ToString(),
                        timestamp_formatted = DateTime.Parse(item["timestamp"]?.ToString() ?? "").ToString("MMM dd, h:mm tt"),
                        status = item["status"]?.ToString().ToUpper(),
                        status_color = GetStatusColor(item["status"]?.ToString())
                    });
                }
                AttendanceHistoryList.ItemsSource = RecentHistory;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Error: {ex.Message}");
            }
        }

        private Color GetStatusColor(string? status) => status switch
        {
            "present" => Color.FromArgb("#2E7D32"),
            "suspicious" => Color.FromArgb("#C62828"),
            "manual_override" => Color.FromArgb("#1565C0"),
            _ => Colors.Gray
        };
    }
}
