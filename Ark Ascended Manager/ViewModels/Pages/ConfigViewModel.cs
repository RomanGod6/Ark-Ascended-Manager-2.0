using System;
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public class ConfigPageViewModel : ObservableObject
    {


        public ConfigPageViewModel()
        {
            // Default constructor, necessary for design-time data or if no parameter is passed
        }

        public void LoadConfig(ServerConfig config)
        {
            
            // Initialize properties based on CurrentConfig
            // ...
        }

        // ...other properties and methods for modifying the server config...
    }
}
