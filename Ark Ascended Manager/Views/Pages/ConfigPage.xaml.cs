using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System; // Ensure System namespace is included for ArgumentNullException
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;
using Newtonsoft.Json;



namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ConfigPage : INavigableView<ConfigPageViewModel> // Make sure the base class is Page
    {

        // Constructor injects the ViewModel and sets it to the DataContext.
        private string fullPathToJson;
        public ConfigPage(ConfigPageViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DisplaySchedules();
            PopulateTimeComboBox();
            LoadSchedulesFromFile();
            



            // It's important to set the DataContext after initializing the components.
            DataContext = ViewModel;
            LoadPluginsToListBox();

            Console.WriteLine("Made it to the config page under the DataContext of Config Page");
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFileName = "allServersSchedulingData.json";
            fullPathToJson = Path.Combine(appNameFolder, jsonFileName);

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

        public class Schedule
        {
            public List<string> Days { get; set; }
            public string Time { get; set; }
            public string DaysAsString => string.Join(", ", Days);
            public bool IsSelected { get; set; }


        }
        // Ensure this method is part of the ConfigPage partial class
        






        public class AllSchedulingData
        {
            public Dictionary<string, List<Schedule>> RestartShutdown { get; set; } = new Dictionary<string, List<Schedule>>();
            public Dictionary<string, List<Schedule>> SaveWorld { get; set; } = new Dictionary<string, List<Schedule>>();
 
        }

        private void SaveScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDays = GetSelectedDays();
            var selectedTime = timeComboBox.SelectedItem as string;

            // Validation: Check if at least one day is selected
            if (!selectedDays.Any())
            {
                System.Windows.MessageBox.Show("Please select at least one day.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validation: Check if a time is selected
            if (string.IsNullOrEmpty(selectedTime))
            {
                System.Windows.MessageBox.Show("Please select a time.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string serverIdentifier = ViewModel.CurrentServerConfig.ServerName;

            // Define the path for the JSON file
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFileName = "allServersSchedulingData.json";
            string fullPathToJson = Path.Combine(appNameFolder, jsonFileName);

            Directory.CreateDirectory(appNameFolder); // Ensure the application directory exists

            // Initialize or load the all scheduling data
            AllSchedulingData allSchedulingData = LoadOrCreateSchedulingData(fullPathToJson);

            // Prepare the new schedule to be added
            var newSchedule = new Schedule
            {
                Days = selectedDays,
                Time = selectedTime
            };

            // Add or update the schedule for the current server
            if (rebootCheckBox.IsChecked == true || shutdownCheckBox.IsChecked == true)
            {
                AddScheduleIfNotDuplicate(allSchedulingData.RestartShutdown, serverIdentifier, newSchedule);
            }

            if (saveCheckBox.IsChecked == true)
            {
                AddScheduleIfNotDuplicate(allSchedulingData.SaveWorld, serverIdentifier, newSchedule);
            }

            // Serialize the updated scheduling data to JSON and write to the file
            SaveSchedulingDataToFile(allSchedulingData, fullPathToJson);

            System.Windows.MessageBox.Show("Schedule saved successfully.", "Success", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void RemoveScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            string serverIdentifier = ViewModel.CurrentServerConfig.ServerName;

            // Load the current schedules
            var allSchedulingData = LoadOrCreateSchedulingData(fullPathToJson);

            // Define criteria to select a schedule for removal. This could be based on the UI, such as a selected item in a DataGrid.
            // For this example, let's assume we're removing based on selected time and days.
            var selectedTime = timeComboBox.SelectedItem as string;
            var selectedDays = GetSelectedDays();

            // Find and remove the schedule
            RemoveSchedule(allSchedulingData, serverIdentifier, selectedDays, selectedTime);

            // Save the updated data
            SaveSchedulingDataToFile(allSchedulingData, fullPathToJson);

            System.Windows.MessageBox.Show("Schedule removed successfully.", "Success", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private List<ScheduleViewModel> GetSchedulesForServer(string serverIdentifier)
        {
            var allSchedulingData = LoadSchedulesFromFile();
            var serverSchedules = new List<ScheduleViewModel>();

            // Assuming your schedules are separated into categories like 'RestartShutdown' and 'SaveWorld'.
            if (allSchedulingData.RestartShutdown.ContainsKey(serverIdentifier))
            {
                serverSchedules.AddRange(allSchedulingData.RestartShutdown[serverIdentifier]
                    .Select(s => new ScheduleViewModel { DaysList = s.Days, Time = s.Time, Action = "Restart/Shutdown" }));
            }

            if (allSchedulingData.SaveWorld.ContainsKey(serverIdentifier))
            {
                serverSchedules.AddRange(allSchedulingData.SaveWorld[serverIdentifier]
                    .Select(s => new ScheduleViewModel { DaysList = s.Days, Time = s.Time, Action = "Save World" }));
            }

            return serverSchedules;
        }
        private void DisplaySchedules()
        {
            string serverIdentifier = ViewModel.CurrentServerConfig.ServerName;
            var schedules = GetSchedulesForServer(serverIdentifier);
            schedulesDataGrid.ItemsSource = schedules;
        }
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            DisplaySchedules();
        }





        private void RemoveSchedule(AllSchedulingData allSchedulingData, string serverIdentifier, List<string> days, string time)
        {
            var schedulesToRemove = new List<Schedule>();

            // Assuming the schedule to be removed could be in either RestartShutdown or SaveWorld
            if (allSchedulingData.RestartShutdown.ContainsKey(serverIdentifier))
            {
                schedulesToRemove.AddRange(allSchedulingData.RestartShutdown[serverIdentifier]
                    .Where(s => s.Time == time && s.Days.SequenceEqual(days)).ToList());
            }

            if (allSchedulingData.SaveWorld.ContainsKey(serverIdentifier))
            {
                schedulesToRemove.AddRange(allSchedulingData.SaveWorld[serverIdentifier]
                    .Where(s => s.Time == time && s.Days.SequenceEqual(days)).ToList());
            }

            foreach (var schedule in schedulesToRemove)
            {
                // Remove from the appropriate lists
                allSchedulingData.RestartShutdown[serverIdentifier].Remove(schedule);
                allSchedulingData.SaveWorld[serverIdentifier].Remove(schedule);
            }
        }

        private AllSchedulingData LoadOrCreateSchedulingData(string fullPathToJson)
        {
            if (File.Exists(fullPathToJson))
            {
                string existingJson = File.ReadAllText(fullPathToJson);
                return JsonConvert.DeserializeObject<AllSchedulingData>(existingJson) ?? new AllSchedulingData();
            }
            return new AllSchedulingData();
        }

        private void SaveSchedulingDataToFile(AllSchedulingData data, string path)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save the schedule: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void AddScheduleIfNotDuplicate(Dictionary<string, List<Schedule>> schedulesDictionary, string serverIdentifier, Schedule newSchedule)
        {
            if (!schedulesDictionary.TryGetValue(serverIdentifier, out var schedules))
            {
                schedules = new List<Schedule>();
                schedulesDictionary[serverIdentifier] = schedules;
            }

            if (!schedules.Any(s => s.Time == newSchedule.Time && s.Days.SequenceEqual(newSchedule.Days)))
            {
                schedules.Add(newSchedule);
            }
        }

        // The IsDuplicateSchedule method is no longer needed separately.


        private AllSchedulingData LoadSchedulesFromFile()
        {
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFileName = "allServersSchedulingData.json";
            string fullPathToJson = Path.Combine(appNameFolder, jsonFileName);

            if (File.Exists(fullPathToJson))
            {
                string json = File.ReadAllText(fullPathToJson);
                return JsonConvert.DeserializeObject<AllSchedulingData>(json);
            }

            return new AllSchedulingData();
        }

        // Call this method when the page loads or when you need to refresh the schedule list
        

        public class ScheduleViewModel
        {
            public string ServerIdentifier { get; set; }
            public string Days => string.Join(", ", DaysList);
            public List<string> DaysList { get; set; }
            public string Time { get; set; }
            public string Action { get; set; } // If you have an action field
            public bool IsSelected { get; set; }

        }








        // Method to get selected days
        private List<string> GetSelectedDays()
        {
            var days = new List<string>();
            if (mondayCheckBox.IsChecked == true) days.Add("Monday");
            if (tuesdayCheckBox.IsChecked == true) days.Add("Tuesday");
            // ... repeat for other days
            if (wednesdayCheckBox.IsChecked == true) days.Add("Wednesday");
            if (thursdayCheckBox.IsChecked == true) days.Add("Thursday");
            if (fridayCheckBox.IsChecked == true) days.Add("Friday");
            if (saturdayCheckBox.IsChecked == true) days.Add("Saturday");
            if (sundayCheckBox.IsChecked == true) days.Add("Sunday");
            return days;
        }
        private void PopulateTimeComboBox()
        {
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    string time = hour.ToString("D2") + ":" + minute.ToString("D2");
                    timeComboBox.Items.Add(time);
                }
            }
        }


        public class SchedulingData
        {
            public string ServerIdentifier { get; set; }
            public List<Schedule> RestartShutdown { get; set; }
            public List<Schedule> SaveWorld { get; set; }
        }








        public void OnNavigatedFrom()
        {
            // If there's anything you need to do when navigating away from this page, do it here.
        }

        // The ViewModel property is publicly accessible and set in the constructor.
        public ConfigPageViewModel ViewModel { get; }

























    }

}