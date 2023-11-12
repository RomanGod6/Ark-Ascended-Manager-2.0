using Wpf.Ui.Controls;
using System.Collections.Generic; // If using collections
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Windows.Input;
using Ark_Ascended_Manager.Views.Pages;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class ServersViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        



        public void OnNavigatedTo()
        {
            // Initialization logic specific to the servers page
        }

        public void OnNavigatedFrom() { }
        public ObservableCollection<ServerConfig> ServerConfigs { get; } = new ObservableCollection<ServerConfig>();


        public void LoadServerConfigs()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var servers = JsonSerializer.Deserialize<List<ServerConfig>>(json);

                if (servers != null)
                {
                    // Clear existing configs before loading new ones
                    ServerConfigs.Clear();

                    foreach (var server in servers)
                    {
                        ServerConfigs.Add(server);
                    }
                }
            }
        }

        // Other methods and properties as needed
    }
}
