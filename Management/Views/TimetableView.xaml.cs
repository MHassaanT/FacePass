using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class TimetableView : UserControl
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _teacherId;

        public TimetableView(Guid teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;
            LoadTimetable();
        }

        private async void LoadTimetable()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                // Fetch timetable slots joined with courses where teacher_id matches
                var url = $"{_baseUrl}/rest/v1/timetable?select=*,courses!inner(name,teacher_id)&courses.teacher_id=eq.{_teacherId}";
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var entries = JArray.Parse(json);

                foreach (JObject entry in entries)
                {
                    entry["course_name"] = entry["courses"]?["name"] ?? "Unknown";
                    entry["start_time_formatted"] = FormatTime(entry["start_time"]?.ToString());
                    entry["end_time_formatted"] = FormatTime(entry["end_time"]?.ToString());
                }

                TimetableGrid.ItemsSource = entries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading timetable: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
