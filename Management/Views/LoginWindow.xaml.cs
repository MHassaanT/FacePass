using System.Windows;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth;

        public LoginWindow()
        {
            InitializeComponent();
            
            // In a real app, these come from configuration
            string url = "https://mfcyozrkizrbrtpfihdj.supabase.co";
            string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
            _auth = new AuthService(url, key);
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            LoginBtn.IsEnabled = false;
            LoginBtn.Content = "Authenticating...";

            var (success, role, userId, name) = await _auth.LoginAsync(EmailBox.Text, PassBox.Password);

            if (success)
            {
                // Navigate to Main Dashboard
                var mainWindow = new MainWindow(role!, name!, userId!.Value);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                LoginBtn.IsEnabled = true;
                LoginBtn.Content = "LOGIN";
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
