using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.IO;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class IntegrationsPage : INavigableView<IntegrationsViewModel>
    {
        private DiscordBotService _botService;
        private readonly IServiceProvider _services; // Add a field to hold IServiceProvider

        public IntegrationsViewModel ViewModel { get; private set; }

        // Ensure IServiceProvider is passed to the constructor
        public IntegrationsPage(IntegrationsViewModel viewModel, IServiceProvider services)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = this;

            _services = services; // Store the provided IServiceProvider
            InitializeBotService(); // Initialize the Discord bot service
        }

        private void InitializeBotService()
        {
            var settings = RetrieveBotSettings();
            if (settings != null && ulong.TryParse(settings.GuildId, out ulong guildId))
            {
                BotTokenTextBox.Text = settings.Token; 
                GuildIdTextBox.Text = settings.GuildId;
                _botService = new DiscordBotService(_services, guildId); // Pass both IServiceProvider and Guild ID
            }
            else
            {
                // Handle error: settings are null or Guild ID is invalid
                // Show an appropriate message to the user or log the error
            }
        }

        private void SaveTokenButton_Click(object sender, RoutedEventArgs e)
        {
            string token = BotTokenTextBox.Text;
            string guildId = GuildIdTextBox.Text;

            // Perform token validation here (if necessary)
            // ...

            // Create an instance of BotSettings
            BotSettings settings = new BotSettings { Token = token, GuildId = guildId };

            // Serialize the settings object to JSON
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

            // Define the path for the JSON file in the AppData folder
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appDataPath, "Ark Ascended Manager");
            string filePath = Path.Combine(folderPath, "botsettings.json");

            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(folderPath);

                // Write the JSON to the file
                File.WriteAllText(filePath, json);

                System.Windows.MessageBox.Show("Token saved successfully.", "Success", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save the token: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }

            
        }

        private async void StartBotButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = RetrieveBotSettings();
            if (settings != null && _botService != null)
            {
                await _botService.StartAsync(settings.Token);
                BotStatusTextBlock.Text = "Bot Status: Connected";
            }
            else
            {
                BotStatusTextBlock.Text = "Bot Status: Error retrieving settings or bot service not initialized";
            }
        }

        public class BotSettings
        {
            public string Token { get; set; }
            public string GuildId { get; set; } // Add this line
        }

        public class TokenManager
        {
            private readonly string _filePath;

            public TokenManager(string filePath)
            {
                _filePath = filePath;
            }

            public void SaveToken(string token)
            {
                var settings = new BotSettings { Token = token };
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }

            
        }


        private async void StopBotButton_Click(object sender, RoutedEventArgs e)
        {
            await _botService.StopAsync();
            BotStatusTextBlock.Text = "Bot Status: Disconnected";
        }
        private BotSettings RetrieveBotSettings()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appDataPath, "Ark Ascended Manager");
            string filePath = Path.Combine(folderPath, "botsettings.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                BotSettings settings = JsonConvert.DeserializeObject<BotSettings>(json);
                return settings; // Return the settings object
            }
            return null; // Return null if the file doesn't exist or the settings can't be read
        }



        // Add other necessary methods here...
    }

    // Define the DiscordBotService class...
}
