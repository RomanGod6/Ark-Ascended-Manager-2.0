using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ark_Ascended_Manager.ViewModels;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private DispatcherTimer _timer;
        private int _currentIndex = 0;

        public DashboardPage()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();

            // Initialize the auto-cycle timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10) // Change interval as needed
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var viewModel = DataContext as DashboardViewModel;
            if (viewModel?.Servers != null && viewModel.Servers.Count > 0)
            {
                _currentIndex = (_currentIndex + 1) % viewModel.Servers.Count;
                viewModel.SelectedServer = viewModel.Servers[_currentIndex];
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _timer.Stop();
            _timer.Start(); // Reset the timer on user interaction
        }

        private void OnRunAsAdminClick(object sender, RoutedEventArgs e)
        {
            // Assuming your executable ends with '.exe' and has the same base name as your DLL
            var exePath = System.IO.Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe");

            var startInfo = new ProcessStartInfo(exePath)
            {
                Verb = "runas",
                UseShellExecute = true
            };

            try
            {
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start as administrator: " + ex.Message);
            }
        }
    }
}
