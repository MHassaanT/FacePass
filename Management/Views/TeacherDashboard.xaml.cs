using System;
using System.Windows;
using System.Windows.Controls;

namespace FacePass.Management.Views
{
    public partial class TeacherDashboard : UserControl
    {
        private readonly long _teacherId;
        private TimetableView? _timetableView;
        private StudentsView? _studentsView;
        private CurrentView? _currentView;

        public TeacherDashboard(long teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;

            // Load default view (Timetable)
            ShowTimetable();
        }

        private void BtnTimetable_Click(object sender, RoutedEventArgs e)
        {
            ShowTimetable();
        }

        private void BtnStudents_Click(object sender, RoutedEventArgs e)
        {
            ShowStudents();
        }

        private void BtnCurrent_Click(object sender, RoutedEventArgs e)
        {
            ShowCurrent();
        }

        private void ShowTimetable()
        {
            // Lazy load TimetableView to optimize resources
            if (_timetableView == null)
            {
                _timetableView = new TimetableView(_teacherId);
            }

            ActiveViewContainer.Content = _timetableView;
            SetSelectedButton(BtnTimetable);
        }

        private void ShowStudents()
        {
            // Lazy load StudentsView
            if (_studentsView == null)
            {
                _studentsView = new StudentsView(_teacherId);
            }

            ActiveViewContainer.Content = _studentsView;
            SetSelectedButton(BtnStudents);
        }

        private void ShowCurrent()
        {
            // Re-instantiate CurrentView each time it's selected to ensure a fresh session timer trigger and initial data load
            _currentView = new CurrentView(_teacherId);

            ActiveViewContainer.Content = _currentView;
            SetSelectedButton(BtnCurrent);
        }

        private void SetSelectedButton(Button selectedButton)
        {
            // Default inactive state styles
            var defaultBg = System.Windows.Media.Brushes.Transparent;
            var defaultFg = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#88FFFFFF")!;

            // Selected active state styles
            var selectedBg = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1A1A1A")!;
            var selectedFg = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#00E676")!;

            // Reset all navigation buttons
            BtnTimetable.Background = defaultBg;
            BtnTimetable.Foreground = defaultFg;
            BtnTimetable.BorderThickness = new Thickness(0);

            BtnStudents.Background = defaultBg;
            BtnStudents.Foreground = defaultFg;
            BtnStudents.BorderThickness = new Thickness(0);

            BtnCurrent.Background = defaultBg;
            BtnCurrent.Foreground = defaultFg;
            BtnCurrent.BorderThickness = new Thickness(0);

            // Set active states on the selected button
            selectedButton.Background = selectedBg;
            selectedButton.Foreground = selectedFg;
            selectedButton.BorderBrush = selectedFg;
            selectedButton.BorderThickness = new Thickness(3, 0, 0, 0); // 3px left border
        }
    }
}
