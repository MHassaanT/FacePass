using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class CurrentView : UserControl
    {
        private readonly long _teacherId;
        private readonly DispatcherTimer _refreshTimer;
        private string _classroomName = "Room 101";

        private static readonly Dictionary<int, string> MethodNames = new()
        {
            [1] = "face",
            [2] = "qr",
            [3] = "manual",
            [4] = "gps_auto"
        };

        private static readonly Dictionary<int, string> StatusNames = new()
        {
            [1] = "present",
            [2] = "suspicious",
            [3] = "manual_override",
            [4] = "absent"
        };

        public CurrentView(long teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;

            LoadRoomName();
            RefreshData();

            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(10);
            _refreshTimer.Tick += Timer_Tick;
            _refreshTimer.Start();
        }

        private async void LoadRoomName()
        {
            try
            {
                using var client = SupabaseRestClient.Create();
                
                var resp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/CLASSROOMS?select=room_number&limit=1");
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
                LastUpdatedText.Text = $"Last updated: {DateTime.Now:hh:mm:ss tt}";
            });
        }

        // Called by the Refresh button in XAML
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private async Task FetchActiveLectureAndAttendance()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var url = $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable?select=*,COURSES!inner(course_id,course_name,teacher_id)&COURSES.teacher_id=eq.{_teacherId}";
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var slots = JArray.Parse(json);

                JObject? activeSlot = null;
                // Use InvariantCulture so the day name is always English (e.g. "Friday")
                // matching the values stored in the DB (e.g. "Monday", "Tuesday", ...).
                string currentDay = DateTime.Now.ToString("dddd", CultureInfo.InvariantCulture);
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
                    var courseName = JsonEmbedHelper.GetField(activeSlot, "COURSES", "course_name");
                    if (string.IsNullOrEmpty(courseName)) courseName = "Unknown Course";
                    var startTime = FormatTime(activeSlot["start_time"]?.ToString());
                    var endTime = FormatTime(activeSlot["end_time"]?.ToString());

                    Dispatcher.Invoke(() => {
                        ActiveCard.Visibility = Visibility.Visible;
                        NoActiveCard.Visibility = Visibility.Collapsed;
                        CourseNameText.Text = courseName;
                        TimeSlotText.Text = $"{startTime} - {endTime}";
                        RoomText.Text = _classroomName;
                    });

                    if (long.TryParse(courseIdStr, out var courseId))
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

        private async Task LoadPresentStudents(long courseId)
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var todayStr = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var url = $"{SupabaseRestClient.BaseUrl}/rest/v1/attendance_logs" +
                          $"?select=*,STUDENTS(USER(first_name,last_name))" +
                          $"&course_id=eq.{courseId}" +
                          $"&timestamp=gte.{todayStr}" +
                          $"&order=timestamp.desc";
                var resp = await client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var logs = JArray.Parse(json);

                // Keep only the latest log per student (logs are DESC so first hit = latest)
                var latestPerStudent = new Dictionary<long, JObject>();
                foreach (JObject log in logs)
                {
                    var sid = log["student_id"]?.Value<long>() ?? -1;
                    if (sid == -1) continue;
                    if (!latestPerStudent.ContainsKey(sid))
                        latestPerStudent[sid] = log;
                }

                var list = new JArray();
                int presentCount = 0;

                foreach (var log in latestPerStudent.Values)
                {
                    var name = JsonEmbedHelper.FullName(log, "STUDENTS", "USER");
                    if (name == "Unknown") name = "Unknown Student";

                    var timeStr = log["timestamp"]?.ToString() ?? "";
                    if (DateTime.TryParse(timeStr, out var timestamp))
                        timeStr = timestamp.ToLocalTime().ToString("hh:mm:ss tt");

                    var methodId = log["method_id"]?.Value<int>() ?? 0;
                    var methodStr = MethodNames.GetValueOrDefault(methodId, "N/A");
                    var formattedMethod = methodStr.ToLower() switch
                    {
                        "face" => "Face Biometrics",
                        "qr" => "QR Scan",
                        "manual" => "Manual Entry",
                        "gps_auto" => "GPS Auto",
                        _ => methodStr
                    };

                    var statusId = log["status_id"]?.Value<int>() ?? 1;
                    var statusStr = StatusNames.GetValueOrDefault(statusId, "present");
                    var formattedStatus = statusStr.ToLower() switch
                    {
                        "present" => "Present",
                        "suspicious" => "Suspicious",
                        "manual_override" => "Manual Override",
                        "absent" => "Absent",
                        _ => statusStr
                    };

                    if (statusStr == "present" || statusStr == "manual_override")
                        presentCount++;

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
            _refreshTimer.Stop();
        }
    }
}
