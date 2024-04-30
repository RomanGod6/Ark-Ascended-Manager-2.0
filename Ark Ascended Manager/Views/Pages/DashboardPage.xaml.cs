using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using Ark_Ascended_Manager.ViewModels;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
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
