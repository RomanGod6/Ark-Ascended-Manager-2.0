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
            var settings = RetrieveBotSettings(); // Make sure this method returns a valid BotSettings object
            if (settings != null)
            {
                // Set UI elements from settings
                BotTokenTextBox.Text = settings.Token ?? "";
                GuildIdTextBox.Text = settings.GuildId ?? "";
                WebhookUrlTextBox.Text = settings.WebhookUrl ?? "";
                VerboseLoggingCheckBox.IsChecked = settings.VerboseLogging;
                LoggerWebhookUrlTextBox.Text = settings.LoggerWebhookUrl ?? "";
                IgnoreMessagesTextBox.Text = settings.IgnoredPatterns != null ? String.Join(Environment.NewLine, settings.IgnoredPatterns) : string.Empty;
                AuthorizedRolesTextBox.Text = settings.AuthorizedRoleIds != null ? String.Join(", ", settings.AuthorizedRoleIds.Select(id => id.ToString())) : "";

                // Initialize the bot service if the Guild ID is valid
                if (ulong.TryParse(settings.GuildId, out ulong guildId))
                {
                    // Assuming BotSettings is adjusted or the constructor of DiscordBotService accepts these parameters
                    _botService = new DiscordBotService(_services, guildId, settings.WebhookUrl, settings);
                }
                else
                {
                    // Log or notify about the invalid Guild ID
                    System.Windows.MessageBox.Show("Invalid Guild ID provided in settings.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                // Handle the scenario where settings couldn't be loaded
                System.Windows.MessageBox.Show("Unable to load settings. Please check your configuration.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            string loggerWebhookUrl = LoggerWebhookUrlTextBox.Text;
            BotSettings settings = new BotSettings
            {
                Token = token,
                GuildId = guildId,
                WebhookUrl = webhookUrl,
                LoggerWebhookUrl = loggerWebhookUrl,
                IgnoredPatterns = ignoredPatterns,
                VerboseLogging = VerboseLoggingCheckBox.IsChecked ?? false,

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
                await _botService.StartAsync(settings);
                BotStatusTextBlock.Text = "Bot Status: Connected";
            }
            else
            {
                BotStatusTextBlock.Text = "Bot Status: Error retrieving settings or bot service not initialized";
            }
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
            if (_botService != null)
            {
                await _botService.StopAsync();
                BotStatusTextBlock.Text = "Bot Status: Disconnected";
            }
            else
            {
                BotStatusTextBlock.Text = "Bot service not initialized";
            }
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
