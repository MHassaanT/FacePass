using FacePass.Management.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FacePass.Management.Views
{
    public partial class TimetableDialog : Window
    {
        private JObject _existingSlot;

        public TimetableDialog(JObject existingSlot = null)
        {
            InitializeComponent();
            _existingSlot = existingSlot;
            LoadData();
        }

        private async void LoadData()
        {
            InitializeTimeCombos();

            try
            {
                using var client = SupabaseRestClient.Create();

                var courseResp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?select=*&order=course_name.asc");
                courseResp.EnsureSuccessStatusCode();
                var courses = JArray.Parse(await courseResp.Content.ReadAsStringAsync());
                foreach (JObject course in courses)
                    course["name"] = course["course_name"];
                CourseCombo.ItemsSource = courses;

                if (_existingSlot != null)
                {
                    var targetDay = _existingSlot["day_of_week"]?.ToString();
                    foreach (ComboBoxItem item in DayCombo.Items)
                    {
                        if (item.Content?.ToString() == targetDay)
                        {
                            DayCombo.SelectedItem = item;
                            break;
                        }
                    }

                    // Use the new combo-based time setters instead of TextBox
                    SetTimeCombos(StartHourCombo, StartMinuteCombo, StartAmPmCombo,
                                  _existingSlot["start_time"]?.ToString() ?? "08:00");
                    SetTimeCombos(EndHourCombo, EndMinuteCombo, EndAmPmCombo,
                                  _existingSlot["end_time"]?.ToString() ?? "09:00");

                    var courseId = _existingSlot["course_id"]?.ToString();
                    foreach (JObject item in CourseCombo.Items)
                    {
                        if (item["course_id"]?.ToString() == courseId)
                        {
                            CourseCombo.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    if (DayCombo.Items.Count > 0)
                        DayCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private void InitializeTimeCombos()
        {
            // Hours 1-12
            var hours = Enumerable.Range(1, 12).Select(h => h.ToString("D2")).ToList();
            // Minutes: 00, 15, 30, 45
            var minutes = new List<string> { "00", "15", "30", "45" };

            StartHourCombo.ItemsSource   = hours;
            StartMinuteCombo.ItemsSource = minutes;
            EndHourCombo.ItemsSource     = hours;
            EndMinuteCombo.ItemsSource   = minutes;

            // Defaults: 08:00 AM to 09:00 AM
            StartHourCombo.SelectedItem   = "08";
            StartMinuteCombo.SelectedItem = "00";
            StartAmPmCombo.SelectedIndex  = 0; // AM

            EndHourCombo.SelectedItem   = "09";
            EndMinuteCombo.SelectedItem = "00";
            EndAmPmCombo.SelectedIndex  = 0; // AM
        }

        /// <summary>
        /// Converts combo selections to 24-hour HH:MM string for database storage.
        /// </summary>
        private string GetTime24(ComboBox hour, ComboBox minute, ComboBox ampm)
        {
            int h = int.Parse(hour.SelectedItem?.ToString() ?? "8");
            int m = int.Parse(minute.SelectedItem?.ToString() ?? "0");
            bool isPm = (ampm.SelectedItem as ComboBoxItem)?.Content?.ToString() == "PM";

            if (isPm && h != 12) h += 12;
            if (!isPm && h == 12) h = 0; // 12 AM = midnight = 00:xx

            return $"{h:D2}:{m:D2}";
        }

        /// <summary>
        /// Populates the combos from a 24-hour HH:MM string (for editing existing slots).
        /// </summary>
        private void SetTimeCombos(ComboBox hour, ComboBox minute, ComboBox ampm, string time24)
        {
            if (!TimeSpan.TryParse(time24, out var ts)) return;

            int h = ts.Hours;
            int m = ts.Minutes;
            bool isPm = h >= 12;

            if (h == 0) h = 12;        // midnight
            else if (h > 12) h -= 12;  // convert to 12-hour

            hour.SelectedItem   = h.ToString("D2");
            minute.SelectedItem = m.ToString("D2");

            // Select the correct minute if it's not a standard interval
            if (minute.SelectedItem == null)
            {
                // Add the exact minute if not in list
                var items = (List<string>)minute.ItemsSource;
                if (!items.Contains(m.ToString("D2")))
                {
                    items.Add(m.ToString("D2"));
                    minute.ItemsSource = null;
                    minute.ItemsSource = items;
                }
                minute.SelectedItem = m.ToString("D2");
            }

            ampm.SelectedIndex = isPm ? 1 : 0;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Get the day string correctly from ComboBoxItem
            string? selectedDay = null;
            if (DayCombo.SelectedItem is ComboBoxItem dayItem)
                selectedDay = dayItem.Content?.ToString();
            else if (DayCombo.SelectedItem is string dayStr)
                selectedDay = dayStr;
            else
                selectedDay = DayCombo.Text;

            if (string.IsNullOrWhiteSpace(selectedDay) || CourseCombo.SelectedItem == null ||
                StartHourCombo.SelectedItem == null || EndHourCombo.SelectedItem == null)
            {
                MessageBox.Show("Please fill all fields.");
                return;
            }

            string startTime = GetTime24(StartHourCombo, StartMinuteCombo, StartAmPmCombo);
            string endTime   = GetTime24(EndHourCombo,   EndMinuteCombo,   EndAmPmCombo);

            if (TimeSpan.Parse(startTime) >= TimeSpan.Parse(endTime))
            {
                MessageBox.Show("End time must be after start time.", "Invalid Time");
                return;
            }

            try
            {
                var course = (JObject)CourseCombo.SelectedItem;
                var courseId = course["course_id"]!.ToString();

                // Check for time overlap on the same day (application-level validation)
                var overlapExists = await CheckTimeOverlapAsync(courseId, selectedDay,
                    startTime, endTime,
                    _existingSlot?["id"]?.ToString());

                if (overlapExists)
                {
                    MessageBox.Show(
                        $"A course is already scheduled on {selectedDay} that overlaps with this time slot.\n\n" +
                        "Please choose a different time.",
                        "Time Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var data = new JObject
                {
                    ["course_id"] = courseId,
                    ["day_of_week"] = selectedDay,
                    ["start_time"] = startTime,
                    ["end_time"] = endTime
                };

                using var client = SupabaseRestClient.Create();
                HttpResponseMessage resp;

                if (_existingSlot != null)
                {
                    var id = _existingSlot["id"]?.ToString();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable?id=eq.{id}")
                    {
                        Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable")
                    {
                        Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show($"Save Error: {(int)resp.StatusCode}\n\n{body}", "Error");
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns true if any existing timetable slot on the same day overlaps
        /// with the proposed start/end time. Excludes the slot being edited (if any).
        /// </summary>
        private async Task<bool> CheckTimeOverlapAsync(
            string courseId, string day, string startTime, string endTime,
            string? excludeId = null)
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                // Fetch all slots on the same day
                var url = $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable" +
                          $"?day_of_week=eq.{Uri.EscapeDataString(day)}&select=id,start_time,end_time";
                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return false;

                var slots = JArray.Parse(await resp.Content.ReadAsStringAsync());

                var newStart = TimeSpan.Parse(startTime);
                var newEnd = TimeSpan.Parse(endTime);

                foreach (var slot in slots)
                {
                    // Skip the slot we're currently editing
                    if (excludeId != null && slot["id"]?.ToString() == excludeId)
                        continue;

                    if (!TimeSpan.TryParse(slot["start_time"]?.ToString(), out var existStart)) continue;
                    if (!TimeSpan.TryParse(slot["end_time"]?.ToString(), out var existEnd)) continue;

                    // Overlap condition: new slot starts before existing ends
                    // AND new slot ends after existing starts
                    if (newStart < existEnd && newEnd > existStart)
                        return true;
                }

                return false;
            }
            catch
            {
                return false; // Don't block saving if the check itself fails
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
