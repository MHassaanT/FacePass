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
            string url = "https://YOUR-PROJECT.supabase.co";
            string key = "YOUR-ANON-KEY";
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
