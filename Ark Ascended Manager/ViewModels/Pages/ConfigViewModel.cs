using System;
using System.Text.Json;
using Ark_Ascended_Manager.Models; // Ensure this is the correct namespace for ServerConfig
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public class ConfigPageViewModel : ObservableObject
    {
        public ServerConfig CurrentServerConfig { get; private set; }

        public ConfigPageViewModel()
        {
            LoadServerProfile();
        }

        private void LoadServerProfile()
        {
            // Define the path where the JSON is saved
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentServerConfig.json");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON from the file
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON to a ServerConfig object
                CurrentServerConfig = JsonSerializer.Deserialize<ServerConfig>(json);

                // Now you can use CurrentServerConfig to get the profile name and find it in the server master list
                // For example:
                // string profileName = CurrentServerConfig.ProfileName;
                // ServerDetails details = FindInMasterList(profileName);
                // ...
            }
        }
        public void LoadConfig(ServerConfig serverConfig)
        {
            // Implementation to set up the ViewModel's properties based on serverConfig
            CurrentServerConfig = serverConfig;
            // ... Set other properties as needed
        }

    }

}
