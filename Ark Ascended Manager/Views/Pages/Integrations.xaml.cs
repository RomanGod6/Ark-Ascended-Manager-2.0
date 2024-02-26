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
        public static IntegrationsPage CurrentInstance { get; private set; }
        public IntegrationsViewModel ViewModel { get; private set; }

        // Ensure IServiceProvider is passed to the constructor
        public IntegrationsPage(IntegrationsViewModel viewModel, IServiceProvider services)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = this;
            CurrentInstance = this;
            _services = services; // Store the provided IServiceProvider
            InitializeBotService(); // Initialize the Discord bot service
        }

        private void InitializeBotService()
        {
            var settings = RetrieveBotSettings();
            if (settings != null)
            {
                // Successfully retrieved settings; proceed to initialize the UI and bot service
                BotTokenTextBox.Text = settings.Token ?? ""; // Using null-coalescing operator for safety
                GuildIdTextBox.Text = settings.GuildId ?? "";
                WebhookUrlTextBox.Text = settings.WebhookUrl ?? "";

                // Check if GuildId is a valid ulong before attempting to parse and use it
                if (ulong.TryParse(settings.GuildId, out ulong guildId))
                {
                    // GuildId is valid; proceed to initialize the bot service
                    _botService = new DiscordBotService(_services, guildId, settings.WebhookUrl);
                }
                else
                {
                    // Invalid GuildId; consider logging this issue or notifying the user
                }

                // Handle IgnoredPatterns, if any
                IgnoreMessagesTextBox.Text = settings.IgnoredPatterns != null
                    ? String.Join(Environment.NewLine, settings.IgnoredPatterns)
                    : string.Empty;

                // Handle AuthorizedRoleIds, if any
                AuthorizedRolesTextBox.Text = settings.AuthorizedRoleIds != null
                    ? String.Join(Environment.NewLine, settings.AuthorizedRoleIds)
                    : string.Empty;
            }
            else
            {
                // Settings could not be retrieved; handle this scenario appropriately
                System.Windows.MessageBox.Show("Unable to load settings. Please check your configuration.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string[] GetIgnoredPatterns()
        {
            // Get the text from the TextBox
            return IgnoreMessagesTextBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
        private void SaveTokenButton_Click(object sender, RoutedEventArgs e)
        {
            string token = BotTokenTextBox.Text;
            string guildId = GuildIdTextBox.Text;
            string webhookUrl = WebhookUrlTextBox.Text;
            var ignoredPatterns = GetIgnoredPatterns();

            // Assuming AuthorizedRolesTextBox.Text contains the role IDs separated by newlines
            var authorizedRoleIds = AuthorizedRolesTextBox.Text
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ulong.Parse) // Convert each ID from string to ulong
                .ToList();

            BotSettings settings = new BotSettings
            {
                Token = token,
                GuildId = guildId,
                WebhookUrl = webhookUrl,
                IgnoredPatterns = ignoredPatterns,
                AuthorizedRoleIds = authorizedRoleIds // Save the authorized role IDs
            };
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

                // Ask the user if they want to restart the application
                System.Windows.MessageBoxResult restartResponse = System.Windows.MessageBox.Show(
                    "Saving these changes requires an application restart. Would you like to restart now?",
                    "Restart Required",
                    System.Windows.MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (restartResponse == System.Windows.MessageBoxResult.Yes)
                {
                    // Restart the application
                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                    Application.Current.Shutdown();
                }
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
            public string GuildId { get; set; }
            public string WebhookUrl { get; set; }
            public string[] IgnoredPatterns { get; set; }
            public List<ulong> AuthorizedRoleIds { get; set; }// Add this line
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
