using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System; // Ensure System namespace is included for ArgumentNullException
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using System.Diagnostics;







namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ConfigPage : INavigableView<ConfigPageViewModel> // Make sure the base class is Page
    {
        private ObservableCollection<string> _headers = new ObservableCollection<string>();

        private readonly INavigationService _navigationService;
        // Constructor injects the ViewModel and sets it to the DataContext.
        private string fullPathToJson;
        public ConfigPage(ConfigPageViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));




            // It's important to set the DataContext after initializing the components.
            DataContext = ViewModel;
            LoadPluginsToListBox();

            Console.WriteLine("Made it to the config page under the DataContext of Config Page");
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFileName = "allServersSchedulingData.json";
            fullPathToJson = Path.Combine(appNameFolder, jsonFileName);



        }
        private void OverrideCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            OverrideTextBox.Visibility = Visibility.Visible;
        }

        private void OverrideCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            OverrideTextBox.Visibility = Visibility.Collapsed;
            UpdateViewModelMapName();
        }


        private void UpdateViewModelMapName()
        {
            var viewModel = DataContext as ConfigPageViewModel;
            if (viewModel != null)
            {
                if (OverrideTextBox.Visibility == Visibility.Collapsed)
                {
                    viewModel.OverrideMapName = "TheIsland_WP"; // Setting default value
                }
                else
                {
                    viewModel.OverrideMapName = OverrideTextBox.Text; // Or any other logic you want
                }
            }
        }




        // Implement the interface member of INavigableView to handle navigation with a parameter.
        public void OnNavigatedTo(object parameter)
        {
            Console.WriteLine("OnNavigatedTo called in ConfigPage.");
            if (parameter is ServerConfig serverConfig)
            {
                Console.WriteLine($"ServerConfig received: {serverConfig.ProfileName}");
                ViewModel.LoadConfig(serverConfig);
            }
            else
            {
                Console.WriteLine("Parameter is not ServerConfig.");
            }
        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();

                e.Handled = true;
            }
        }


        private void LoadPluginsToListBox()
        {
            // Retrieve the path from the ViewModel
            string pluginsDirectoryPath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins");

            if (Directory.Exists(pluginsDirectoryPath))
            {
                // Fetch all directories which represent plugins
                var pluginDirectories = Directory.GetDirectories(pluginsDirectoryPath);

                // This will hold the names of the plugins
                var pluginNames = pluginDirectories.Select(Path.GetFileName).ToList();

                // Bind the ListBox's ItemsSource to the plugin names
                lstPlugins.ItemsSource = pluginNames;
            }
        }


        private void LstPlugins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Assuming the ListBox's SelectionMode is Single
            var selectedPlugin = lstPlugins.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedPlugin))
            {
                // Construct the path to the selected plugin's config.json
                string configFilePath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins", selectedPlugin, "config.json");

                if (File.Exists(configFilePath))
                {
                    // Read the JSON file and set the text editor's content
                    jsonEditor.Text = File.ReadAllText(configFilePath);
                }
            }
        }
        private void SaveJsonButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPlugin = lstPlugins.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedPlugin))
            {
                System.Windows.MessageBox.Show("Please select a plugin to save.");
                return;
            }

            var result = System.Windows.MessageBox.Show("Do you want to save the configuration to all servers?", "Save Configuration", System.Windows.MessageBoxButton.YesNoCancel);

            if (result == System.Windows.MessageBoxResult.Cancel)
            {
                return;
            }

            string jsonContent = jsonEditor.Text;
            List<string> skippedServers = new List<string>(); // List to keep track of skipped servers

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // User chose to save to all servers
                var serverPaths = GetAllServerPaths();
                foreach (var serverPath in serverPaths)
                {
                    if (!SaveJsonToServerPlugin(serverPath, selectedPlugin, jsonContent))
                    {
                        // If the return value is false, it means the save was skipped
                        skippedServers.Add(serverPath);
                    }
                }
            }
            else
            {
                // User chose to save only to the current server
                SaveJsonToServerPlugin(ViewModel.CurrentServerConfig.ServerPath, selectedPlugin, jsonContent);
            }

            // Notify the user of skipped servers
            if (skippedServers.Any())
            {
                string skippedMessage = "The following servers were skipped because the plugin folder was not found:\n" + string.Join("\n", skippedServers);
                System.Windows.MessageBox.Show(skippedMessage, "Skipped Servers", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public static class NavigationContext
        {
            public static string CurrentServerProfileName { get; set; }
        }

        private void AddNewScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            string profileName = ViewModel.CurrentServerConfig.ProfileName;
            SaveCurrentServerProfileNameToJson(profileName);
            _navigationService.Navigate(typeof(CreateSchedulePage));
        }
        private void SaveCurrentServerProfileNameToJson(string serverProfileName)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentscheduleserver.json");
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var jsonData = JsonConvert.SerializeObject(new { ServerProfileName = serverProfileName });
            File.WriteAllText(path, jsonData);
        }





        private List<string> GetAllServerPaths()
        {
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string serversFilePath = Path.Combine(appDataFolderPath, "Ark Ascended Manager", "servers.json");
            if (File.Exists(serversFilePath))
            {
                string json = File.ReadAllText(serversFilePath);
                var servers = JsonConvert.DeserializeObject<List<ServerConfig>>(json);
                return servers.Select(s => s.ServerPath).ToList();
            }
            return new List<string>();
        }

        private bool SaveJsonToServerPlugin(string serverPath, string pluginName, string jsonContent)
        {
            string pluginConfigPath = Path.Combine(serverPath, "ShooterGame", "Binaries", "Win64", "ArkApi", "Plugins", pluginName, "config.json");

            // Check if the plugin directory exists
            if (Directory.Exists(Path.GetDirectoryName(pluginConfigPath)))
            {
                try
                {
                    File.WriteAllText(pluginConfigPath, jsonContent);
                    return true; // Operation was successful
                }
                catch (Exception ex)
                {
                    // Handle any exceptions (e.g., log them)
                    return false; // Operation failed
                }
            }
            else
            {
                return false; // Directory does not exist, operation skipped
            }
        }








        private void SearchButtonGameini_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBoxGameIni.Text.ToLower();
            SearchAndBringIntoView(Gameini, searchText);
        }

        private void SearchAndBringIntoView(StackPanel stackPanel, string searchText)
        {
            foreach (var child in stackPanel.Children)
            {
                if (child is Label label && label.Content.ToString().ToLower().Contains(searchText))
                {
                    label.BringIntoView();
                    break; // Exit the loop after the first match is found
                }
            }
        }



        public void OnNavigatedFrom()
        {
            // If there's anything you need to do when navigating away from this page, do it here.
        }

        // The ViewModel property is publicly accessible and set in the constructor.
        public ConfigPageViewModel ViewModel { get; }

























    }

}