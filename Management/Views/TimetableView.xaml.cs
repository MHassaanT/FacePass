using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class TimetableView : UserControl
    {
        private readonly long _teacherId;

        public TimetableView(long teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;
            LoadTimetable();
        }

        private async void LoadTimetable()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var url = $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable?select=*,COURSES!inner(course_name,teacher_id)&COURSES.teacher_id=eq.{_teacherId}";
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var entries = JArray.Parse(json);

                foreach (JObject entry in entries)
                {
                    entry["course_name"] = JsonEmbedHelper.GetField(entry, "COURSES", "course_name");
                    if (string.IsNullOrEmpty(entry["course_name"]?.ToString()))
                        entry["course_name"] = "Unknown";
                    entry["start_time_formatted"] = FormatTime(entry["start_time"]?.ToString());
                    entry["end_time_formatted"]   = FormatTime(entry["end_time"]?.ToString());
                }

                TimetableGrid.ItemsSource = entries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading timetable: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatTime(string? timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) return "N/A";
            if (TimeSpan.TryParse(timeStr, out var timeSpan))
            {
                var dateTime = DateTime.Today.Add(timeSpan);
                return dateTime.ToString("hh:mm tt");
            }
            return timeStr;
        }
    }
}