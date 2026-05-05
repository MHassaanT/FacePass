using System;
using System.Windows;
using FacePass.Management.Views;

namespace FacePass.Management
{
    public partial class MainWindow : Window
    {
        public MainWindow(string role, string userName, Guid userId)
        {
            InitializeComponent();
            
            UserNameLabel.Text = $"Welcome, {userName}";
            RoleLabel.Text = $"| {role.ToUpper()} PORTAL";

            // Role-based view switching
            if (role.ToLower() == "admin")
            {
                MainContent.Content = new AdminDashboard(userId);
            }
            else if (role.ToLower() == "teacher")
            {
                MainContent.Content = new TeacherDashboard();
            }
            else
            {
                MessageBox.Show("Unauthorized role.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}
