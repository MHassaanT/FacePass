using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class StudentsView : UserControl
    {
        private readonly long _teacherId;

        public event Action<long, bool>? StudentSelected;

        public StudentsView(long teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;
            // Run after layout is complete to avoid dispatcher crashes on first load
            Dispatcher.BeginInvoke(new Action(async () => await LoadStudentsAsync()));
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                StudentsGrid.ItemsSource = null;
                using var client = SupabaseRestClient.Create();

                // ── Step 1: courses for this teacher ────────────────────────
                List<string> courseIds;
                try
                {
                    var courseResp = await client.GetAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES" +
                        $"?teacher_id=eq.{_teacherId}&select=course_id");
                    courseResp.EnsureSuccessStatusCode();

                    var courseArray = JArray.Parse(await courseResp.Content.ReadAsStringAsync());
                    courseIds = courseArray
                        .Select(c => c["course_id"]?.ToString())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .ToList()!;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[Step 1 - Courses] {ex.Message}");
                    return;
                }

                if (courseIds.Count == 0)
                {
                    StudentsGrid.ItemsSource = null;
                    return;
                }

                var courseIdList = string.Join(",", courseIds);

                // ── Step 2: enrollments ──────────────────────────────────────
                List<string> studentIds;
                try
                {
                    var enrollResp = await client.GetAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSE_ENROLLMENTS" +
                        $"?course_id=in.({courseIdList})&select=student_id");
                    enrollResp.EnsureSuccessStatusCode();

                    var enrollArray = JArray.Parse(await enrollResp.Content.ReadAsStringAsync());
                    studentIds = enrollArray
                        .Select(e => e["student_id"]?.ToString())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct()
                        .ToList()!;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[Step 2 - Enrollments] {ex.Message}");
                    return;
                }

                if (studentIds.Count == 0)
                {
                    StudentsGrid.ItemsSource = null;
                    return;
                }

                var studentIdList = string.Join(",", studentIds);

                // ── Step 3: USER rows ────────────────────────────────────────
                Dictionary<string, JObject> userMap;
                try
                {
                    var userResp = await client.GetAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/USER" +
                        $"?user_id=in.({studentIdList})" +
                        $"&select=user_id,first_name,last_name,email,created_at");
                    userResp.EnsureSuccessStatusCode();

                    var userArray = JArray.Parse(await userResp.Content.ReadAsStringAsync());
                    userMap = userArray
                        .OfType<JObject>()
                        .ToDictionary(u => u["user_id"]!.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[Step 3 - Users] {ex.Message}");
                    return;
                }

                // ── Step 4: face encodings ───────────────────────────────────
                var faceEnrolledIds = new HashSet<string>();
                try
                {
                    var faceResp = await client.GetAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/FACE_ENCODINGS" +
                        $"?student_id=in.({studentIdList})&select=student_id");
                    faceResp.EnsureSuccessStatusCode();

                    var faceArray = JArray.Parse(await faceResp.Content.ReadAsStringAsync());
                    foreach (var row in faceArray)
                        faceEnrolledIds.Add(row["student_id"]!.ToString());
                }
                catch (Exception ex)
                {
                    // Non-fatal: show empty biometric status if this fails
                    System.Diagnostics.Debug.WriteLine($"[Step 4 - FaceEncodings] {ex.Message}");
                }

                // ── Step 5: build list ───────────────────────────────────────
                var studentList = new List<object>();
                try
                {
                    foreach (var sid in studentIds)
                    {
                        if (!userMap.TryGetValue(sid, out var user)) continue;

                        var first = user["first_name"]?.ToString() ?? "";
                        var last  = user["last_name"]?.ToString()  ?? "";
                        var name  = $"{first} {last}".Trim();
                        if (string.IsNullOrEmpty(name)) name = "Unknown";

                        var email       = user["email"]?.ToString() ?? "Unknown";
                        var createdStr  = user["created_at"]?.ToString() ?? "";
                        var dateCreated = DateTime.TryParse(createdStr, out var dt)
                            ? dt.ToString("dd MMM yyyy") : "";
                        bool enrolled   = faceEnrolledIds.Contains(sid);

                        studentList.Add(new
                        {
                            Id                = long.Parse(sid),
                            Name              = name,
                            Email             = email,
                            DateCreated       = dateCreated,
                            BiometricEnrolled = enrolled
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[Step 5 - BuildList] {ex.Message}");
                    return;
                }

                // ── Step 6: bind to DataGrid ─────────────────────────────────
                try
                {
                    StudentsGrid.ItemsSource = studentList;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"[Step 6 - DataGrid Bind] {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"[StudentsView - Unhandled]\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                    "Crash Diagnostic", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(async () => await LoadStudentsAsync()));
        }

        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null)
            {
                RegisterBiometricBtn.IsEnabled = false;
                RegisterBiometricBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5252"));
                return;
            }
            try
            {
                dynamic dyn   = StudentsGrid.SelectedItem;
                long id       = dyn.Id;
                bool enrolled = dyn.BiometricEnrolled;
                StudentSelected?.Invoke(id, enrolled);

                if (!enrolled)
                {
                    RegisterBiometricBtn.IsEnabled = true;
                    RegisterBiometricBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00E676"));
                }
                else
                {
                    RegisterBiometricBtn.IsEnabled = false;
                    RegisterBiometricBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5252"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudentsView SelectionChanged] {ex.Message}");
            }
        }

        private void RegisterBiometric_Click(object sender, RoutedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null) return;
            dynamic dyn = StudentsGrid.SelectedItem;
            long id = dyn.Id;
            string name = dyn.Name;

            var dialog = new BiometricRegistrationDialog(id, name)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                Dispatcher.BeginInvoke(new Action(async () => await LoadStudentsAsync()));
            }
        }
    }
}