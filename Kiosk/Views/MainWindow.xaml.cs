using System;
using System.Windows;
using System.Windows.Threading;
using FacePass.Kiosk.Services;
using FacePass.Kiosk.ViewModels;

namespace FacePass.Kiosk.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;
        private readonly DispatcherTimer _clockTimer;

        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;

            // Update clock every second
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) =>
            {
                ClockText.Text = DateTime.Now.ToString("dddd, dd MMM yyyy   HH:mm:ss");
            };
            _clockTimer.Start();
            ClockText.Text = DateTime.Now.ToString("dddd, dd MMM yyyy   HH:mm:ss");
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            _vm.Dispose();
            base.OnClosed(e);
        }
    }
}
