using Wpf.Ui.Controls;
using System.Collections.Generic; // If using collections
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Windows.Input;
using Ark_Ascended_Manager.Views.Pages;
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class ServersViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        public ICommand SaveServerProfileCommand { get; }

        public ServersViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            SaveServerProfileCommand = new RelayCommand<ServerConfig>(SaveServerProfileAndNavigate);
            // Other initializations
        }

        public void OnNavigatedTo()
        {
            // Initialization logic specific to the servers page
        }

        public void OnNavigatedFrom() { }
        public ObservableCollection<ServerConfig> ServerConfigs { get; } = new ObservableCollection<ServerConfig>();

        // This method would be called when a server card is clicked


        private void SaveServerProfileAndNavigate(ServerConfig serverConfig)
        {
            // Define the path where you want to save the JSON
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentServerConfig.json");

            // Serialize the ServerConfig object to JSON
            string json = JsonSerializer.Serialize(serverConfig);

            // Write the JSON to a file
            File.WriteAllText(filePath, json);

            // Navigate to the ConfigPage
            _navigationService.Navigate(typeof(ConfigPage));
        }


        public void LoadServerConfigs()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "servers.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var servers = JsonSerializer.Deserialize<List<ServerConfig>>(json);

                if (servers != null)
                {
                    ServerConfigs.Clear(); // Clear existing configs before loading new ones
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
