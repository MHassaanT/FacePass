using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class CurrentView : UserControl
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _teacherId;
        private readonly DispatcherTimer _refreshTimer;
        private string _classroomName = "Room 101"; // Default room name fallback

        public CurrentView(Guid teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;

            // Load initial dynamic room number from Supabase classrooms
            LoadRoomName();

            // Fetch initial active slot and logs
            RefreshData();

            // Set up dispatcher timer for 30s auto-refresh
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(30);
            _refreshTimer.Tick += Timer_Tick;
            _refreshTimer.Start();
        }

        private async void LoadRoomName()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);
                
                // Fetch first classroom's room_number from database
                var resp = await client.GetAsync($"{_baseUrl}/rest/v1/classrooms?select=room_number&limit=1");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var arr = JArray.Parse(json);
                    if (arr.Count > 0 && arr[0]["room_number"] != null)
                    {
                        _classroomName = $"Room {arr[0]["room_number"]}";
                    }
                }
            }
            catch
            {
                // Silent fallback to default Room 101
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            RefreshData();
        }

        private async void RefreshData()
        {
            Dispatcher.Invoke(() => {
                LastUpdatedText.Text = "Syncing...";
            });

            await FetchActiveLectureAndAttendance();

            Dispatcher.Invoke(() => {
                LastUpdatedText.Text = $"Last updated: {DateTime.Now.ToString("hh:mm:ss tt")}";
            });
        }

        private async Task FetchActiveLectureAndAttendance()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                // 1. Fetch timetable slots for this teacher
                var url = $"{_baseUrl}/rest/v1/timetable?select=*,courses!inner(id,name,teacher_id)&courses.teacher_id=eq.{_teacherId}";
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var slots = JArray.Parse(json);

                JObject? activeSlot = null;
                string currentDay = DateTime.Now.ToString("dddd"); // e.g. "Friday"
                TimeSpan currentTime = DateTime.Now.TimeOfDay;

                foreach (JObject slot in slots)
                {
                    string? slotDay = slot["day_of_week"]?.ToString();
                    string? startTimeStr = slot["start_time"]?.ToString();
                    string? endTimeStr = slot["end_time"]?.ToString();

                    if (slotDay == currentDay && !string.IsNullOrEmpty(startTimeStr) && !string.IsNullOrEmpty(endTimeStr))
                    {
                        if (TimeSpan.TryParse(startTimeStr, out var startTime) && TimeSpan.TryParse(endTimeStr, out var endTime))
                        {
                            if (currentTime >= startTime && currentTime <= endTime)
                            {
                                activeSlot = slot;
                                break;
                            }
                        }
                    }
                }

                if (activeSlot != null)
                {
                    var courseIdStr = activeSlot["course_id"]?.ToString();
                    var courseName = activeSlot["courses"]?["name"]?.ToString() ?? "Unknown Course";
                    var startTime = FormatTime(activeSlot["start_time"]?.ToString());
                    var endTime = FormatTime(activeSlot["end_time"]?.ToString());

                    Dispatcher.Invoke(() => {
                        ActiveCard.Visibility = Visibility.Visible;
                        NoActiveCard.Visibility = Visibility.Collapsed;
                        CourseNameText.Text = courseName;
                        TimeSlotText.Text = $"{startTime} - {endTime}";
                        RoomText.Text = _classroomName;
                    });

                    if (Guid.TryParse(courseIdStr, out var courseId))
                    {
                        await LoadPresentStudents(courseId);
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => {
                        ActiveCard.Visibility = Visibility.Collapsed;
                        NoActiveCard.Visibility = Visibility.Visible;
                        PresentStudentsGrid.ItemsSource = null;
                        PresentCountText.Text = "0";
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CurrentView] Fetch Active Lecture Error: {ex.Message}");
            }
        }

        private async Task LoadPresentStudents(Guid courseId)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                // Fetch today's logs for this course
                var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var url = $"{_baseUrl}/rest/v1/attendance_logs?course_id=eq.{courseId}&timestamp=gte.{todayStr}&select=*,students(users(*))&order=timestamp.desc";
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var logs = JArray.Parse(json);

                var list = new JArray();
                int presentCount = 0;

                foreach (JObject log in logs)
                {
                    var student = log["students"] as JObject;
                    var user = student?["users"] as JObject;
                    
                    var name = user?["name"]?.ToString() ?? 
                               ((user?["first_name"] != null) ? $"{user["first_name"]} {user["last_name"]}" : "Unknown Student");
                    
                    var timeStr = log["timestamp"]?.ToString() ?? "";
                    if (DateTime.TryParse(timeStr, out var timestamp))
                    {
                        timeStr = timestamp.ToLocalTime().ToString("hh:mm:ss tt");
                    }

                    var methodStr = log["method"]?.ToString() ?? "N/A";
                    var formattedMethod = methodStr.ToLower() switch
                    {
                        "face" => "Face Biometrics",
                        "qr" => "QR Scan",
                        "manual" => "Manual Entry",
                        _ => methodStr
                    };

                    var statusStr = log["status"]?.ToString() ?? "present";
                    var formattedStatus = statusStr.ToLower() switch
                    {
                        "present" => "Present",
                        "suspicious" => "Suspicious",
                        "manual_override" => "Manual Override",
                        _ => statusStr
                    };

                    if (statusStr.ToLower() == "present" || statusStr.ToLower() == "manual_override")
                    {
                        presentCount++;
                    }

                    var flatLog = new JObject
                    {
                        ["student_name"] = name,
                        ["timestamp"] = timeStr,
                        ["method"] = methodStr,
                        ["method_formatted"] = formattedMethod,
                        ["status"] = statusStr,
                        ["status_formatted"] = formattedStatus
                    };
                    list.Add(flatLog);
                }

                Dispatcher.Invoke(() => {
                    PresentStudentsGrid.ItemsSource = list;
                    PresentCountText.Text = presentCount.ToString();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CurrentView] Present Students Load Error: {ex.Message}");
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

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Stop timer to prevent memory leaks when control is unloaded
            _refreshTimer.Stop();
        }
    }
}
