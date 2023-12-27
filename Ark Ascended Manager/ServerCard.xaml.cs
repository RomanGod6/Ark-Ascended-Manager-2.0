using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using Ark_Ascended_Manager.Models; // Include the namespace for ServerProfile
using System.Windows.Media;
namespace Ark_Ascended_Manager.Views.Controls
{
    public partial class ServerCard : UserControl
    {
        private ServerProfile _serverProfile;
        public ServerCard()
        {
            InitializeComponent();
            
        }
        
        


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var server = DataContext as ServerProfile;
            if (server != null)
            {
                // Implement the logic to launch the server
                // Example: Process.Start(server.ServerPath);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var server = DataContext as ServerProfile;
            // Implement the logic to stop the server
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var server = DataContext as ServerProfile;
            // Implement the logic to update the server
        }
        

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            var server = DataContext as ServerProfile;
            if (server != null)
            {
                try
                {
                    if (Directory.Exists(server.ServerPath))
                    {
                        Process.Start("explorer.exe", server.ServerPath);
                    }
                    else
                    {
                        MessageBox.Show("Directory does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
        }

    }
    public class ServerStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Assuming 'Online' and 'Servers Up To Date' should be green
            if (value?.ToString() == "Online" || value?.ToString() == "Servers Up To Date")
            {
                return Brushes.Green;
            }
            // Assuming 'Offline' and 'Server is Not Up to Date' should be red
            else if (value?.ToString() == "Offline" || value?.ToString() == "Server is Not Up to Date")
            {
                return Brushes.Red;
            }
            // Default color if status is something else or null
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
