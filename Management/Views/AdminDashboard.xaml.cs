using System.Windows;
using System.Windows.Controls;

namespace FacePass.Management.Views
{
    public partial class AdminDashboard : UserControl
    {
        public AdminDashboard()
        {
            InitializeComponent();
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                RefreshUsers_Click(null!, null!);
            }
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                // Logic to fetch from Supabase
                // var users = await _supabase.GetUsers();
                // UsersGrid.ItemsSource = users;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
