using System.Windows;
using System.Windows.Controls;

namespace FacePass.Management.Views
{
    public partial class TeacherDashboard : UserControl
    {
        public TeacherDashboard()
        {
            InitializeComponent();
        }

        private void CaptureFace_Click(object sender, RoutedEventArgs e)
        {
            // Logic to open camera, detect face, and update Supabase BYTEA
            MessageBox.Show("Biometric Capture Started...", "Registration", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ManualOverride_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Manual Override Successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
