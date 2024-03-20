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
using System.Windows.Documents;
using System.Windows.Media;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Threading;







namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ConfigPage : INavigableView<ConfigPageViewModel> // Make sure the base class is Page
    {
        private List<SpawnClassEntry> spawnClassEntries;
        private List<CreatureIdEntry> creatureIdEntries;
        private ObservableCollection<string> _headers = new ObservableCollection<string>();
        // Inside your ConfigPageViewModel class
        public ObservableCollection<StackSizeOverride> StackSizeOverrides { get; set; } = new ObservableCollection<StackSizeOverride>();

        private ICollectionView engramCollectionView;
        private string stackOverridesPath;
        private readonly INavigationService _navigationService;
        // Constructor injects the ViewModel and sets it to the DataContext.
        private string fullPathToJson;
        public ConfigPage(ConfigPageViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            stackOverridesPath = Path.Combine(viewModel.CurrentServerConfig.ServerPath, "overrides", "stacking.json");
            // Using System.IO and System.Environment to construct the path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Debug.WriteLine(appDataPath);
            var dinoBPsPath = Path.Combine(appDataPath, "Ark Ascended Manager", "Data", "DinoBPs");
            Debug.WriteLine(dinoBPsPath);
            var spawnClassesPath = Path.Combine(dinoBPsPath, "Spawns.json"); 
            Debug.WriteLine(spawnClassesPath);
            var creatureIdsPath = Path.Combine(dinoBPsPath, "Dinos.json"); 
            try
            {
                var spawnClassContent = File.ReadAllText(spawnClassesPath);
                var spawnClassEntries = JsonConvert.DeserializeObject<List<SpawnClassEntry>>(spawnClassContent);
                var creatureIdContent = File.ReadAllText(creatureIdsPath);
                var creatureIdEntries = JsonConvert.DeserializeObject<List<CreatureIdEntry>>(creatureIdContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }




            var folderPath = Path.GetDirectoryName(stackOverridesPath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }



            // It's important to set the DataContext after initializing the components.
            DataContext = ViewModel;
            LoadPluginsToListBox();
            UpdateRichTextPreview(ViewModel.MOTD);
            Console.WriteLine("Made it to the config page under the DataContext of Config Page");
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFileName = "allServersSchedulingData.json";
            fullPathToJson = Path.Combine(appNameFolder, jsonFileName);
            LoadStackSizeOverrides();
            LoadStackSizeOverridesConfigs();
            LoadAndMergeEngrams();
            InitializeCheckBoxStates();
            InitializeCheckBoxState();
            engramCollectionView = CollectionViewSource.GetDefaultView(ViewModel.EngramOverrides);
            dgEngramOverrides.ItemsSource = engramCollectionView;



        }

        private void AddDinoOverride_Click(object sender, RoutedEventArgs e)
        {
            // Example of dynamically adding UI elements for the new override configuration
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            var spawnClassComboBox = new ComboBox();
            spawnClassComboBox.ItemsSource = spawnClassEntries; // Assuming this is already loaded
            spawnClassComboBox.DisplayMemberPath = "Name";

            var creatureIdComboBox = new ComboBox();
            creatureIdComboBox.ItemsSource = creatureIdEntries; // Assuming this is already loaded
            creatureIdComboBox.DisplayMemberPath = "Name";

            var spawnNameTextBox = new Wpf.Ui.Controls.TextBox { Width = 100 };
            var factorTextBox = new Wpf.Ui.Controls.TextBox { Width = 100 };
            var percentageTextBox = new Wpf.Ui.Controls.TextBox { Width = 100 };

            // Add the controls to the StackPanel
            stackPanel.Children.Add(spawnClassComboBox);
            stackPanel.Children.Add(creatureIdComboBox);
            stackPanel.Children.Add(spawnNameTextBox);
            stackPanel.Children.Add(factorTextBox);
            stackPanel.Children.Add(percentageTextBox);

            // Add the StackPanel to the parent container (e.g., dinoOverridePanel)
            dinoOverridePanel.Children.Add(stackPanel);
        }


        public class SpawnClassEntry
        {
            public string Name { get; set; }
            public string ClassString { get; set; }
        }

        public class CreatureIdEntry
        {
            public string Name { get; set; }
            public string Id { get; set; }
        }













        public void LoadAndMergeEngrams()
        {
            // Path where the JSON files are located
            string engramsJsonPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ark Ascended Manager", "Data", "Engrams");

            // Clear existing collection
            ViewModel.EngramOverrides.Clear();

            // Load all JSON files
            var jsonFiles = Directory.GetFiles(engramsJsonPath, "*.json");

            // Deserialization structure to match the JSON file structure
            List<Dictionary<string, List<EngramOverride>>> allEngrams = new List<Dictionary<string, List<EngramOverride>>>();

            foreach (var filePath in jsonFiles)
            {
                string jsonContent = File.ReadAllText(filePath);
                var engramsFromJson = JsonConvert.DeserializeObject<List<Dictionary<string, List<EngramOverride>>>>(jsonContent);
                if (engramsFromJson != null)
                {
                    allEngrams.AddRange(engramsFromJson);
                }
            }

            // Flatten the list of dictionaries into a single list of EngramOverrides
            var flattenedEngrams = allEngrams.SelectMany(dict => dict["Engrams"]).ToList();

            // Load the game.ini file
            string gameIniPath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath,
                                              "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");

            if (File.Exists(gameIniPath))
            {
                var gameIniContents = File.ReadAllLines(gameIniPath);

                // Process game.ini contents and update allEngrams with any overrides found
                foreach (var engram in flattenedEngrams)
                {
                    string lineToFind = $"OverrideNamedEngramEntries=(EngramClassName=\"{engram.EngramClassName}\"";
                    string foundLine = gameIniContents.FirstOrDefault(line => line.Contains(lineToFind));

                    if (foundLine != null)
                    {
                        // Split the line into components
                        string[] components = foundLine.Split(',');

                        foreach (string component in components)
                        {
                            string[] keyValue = component.Split('=');
                            if (keyValue.Length == 2) // Make sure there is a key and a value
                            {
                                string key = keyValue[0].Trim();
                                string value = keyValue[1].Trim().TrimEnd(')'); // Trim the ending parenthesis if present

                                switch (key)
                                {
                                    case "EngramHidden":
                                        if (bool.TryParse(value, out bool hiddenValue))
                                        {
                                            engram.EngramHidden = hiddenValue;
                                        }
                                        break;
                                    case "EngramPointsCost":
                                        if (int.TryParse(value, out int pointsCostValue))
                                        {
                                            engram.EngramPointsCost = pointsCostValue;
                                        }
                                        break;
                                    case "EngramLevelRequirement":
                                        if (int.TryParse(value, out int levelRequirementValue))
                                        {
                                            engram.EngramLevelRequirement = levelRequirementValue;
                                        }
                                        break;
                                    case "RemoveEngramPreReq":
                                        if (bool.TryParse(value, out bool removePreReqValue))
                                        {
                                            engram.RemoveEngramPreReq = removePreReqValue;
                                        }
                                        break;
                                        // Repeat for other properties as needed
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"No override found in Game.ini for {engram.EngramClassName}");
                    }


                }

                // Process AutoUnlocks from game.ini
                foreach (var line in gameIniContents)
                {
                    if (line.StartsWith("EngramEntryAutoUnlocks=("))
                    {
                        var autoUnlockPattern = @"EngramClassName=""([^""]+)"",LevelToAutoUnlock=(\d+)";
                        var match = Regex.Match(line, autoUnlockPattern);
                        if (match.Success)
                        {
                            var engramClassName = match.Groups[1].Value;
                            var levelToAutoUnlock = int.Parse(match.Groups[2].Value);

                            var engram = flattenedEngrams.FirstOrDefault(e => e.EngramClassName == engramClassName);
                            if (engram != null)
                            {
                                engram.AutoUnlock = true;
                                engram.LevelToAutoUnlock = levelToAutoUnlock;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine($"Game.ini file not found at path: {gameIniPath}");
            }

            // Update the ObservableCollection that is bound to the UI
            ViewModel.EngramOverrides = new ObservableCollection<EngramOverride>(flattenedEngrams);
}

        private void SaveEngramsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveEngramsToIni();
        }
        private void SaveEngramsToIni()
        {
            string iniFilePath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            string sectionHeader = "[/script/shootergame.shootergamemode]";

            // Read the existing ini file into a list of strings
            var iniLines = File.ReadAllLines(iniFilePath).ToList();

            // Find the index of the section header
            int sectionIndex = iniLines.FindIndex(line => line.Trim().Equals(sectionHeader));

            // Remove existing engram entries to avoid duplicates
            iniLines.RemoveAll(line => line.StartsWith("OverrideNamedEngramEntries=") || line.StartsWith("EngramEntryAutoUnlocks="));

            // Add section header if it does not existLoadAndMergeEngrams
            if (sectionIndex == -1)
            {
                iniLines.Add(sectionHeader);
                sectionIndex = iniLines.Count - 1;
            }

            // Append new engram override entries
            foreach (var engramOverride in ViewModel.EngramOverrides)
            {
                string overrideLine = $"OverrideNamedEngramEntries=(EngramClassName=\"{engramOverride.EngramClassName}\",EngramHidden={engramOverride.EngramHidden.ToString().ToLower()},EngramPointsCost={engramOverride.EngramPointsCost},EngramLevelRequirement={engramOverride.EngramLevelRequirement},RemoveEngramPreReq={(engramOverride.RemoveEngramPreReq.HasValue ? engramOverride.RemoveEngramPreReq.Value.ToString().ToLower() : "false")})";
                iniLines.Insert(++sectionIndex, overrideLine);

                // Handle auto-unlock separately
                if (engramOverride.AutoUnlock)
                {
                    string autoUnlockLine = $"EngramEntryAutoUnlocks=(EngramClassName=\"{engramOverride.EngramClassName}\",LevelToAutoUnlock={engramOverride.LevelToAutoUnlock})";
                    iniLines.Insert(++sectionIndex, autoUnlockLine);
                }
            }

            // Write back to the Game.ini file
            File.WriteAllLines(iniFilePath, iniLines);
        }








        public class EngramOverride
        {
            public int? EngramIndex { get; set; }
            public string EngramClassName { get; set; }
            public bool? EngramHidden { get; set; }
            public int? EngramPointsCost { get; set; }
            public int? EngramLevelRequirement { get; set; }
            public bool? RemoveEngramPreReq { get; set; } = false; // Default to false
            public bool AutoUnlock { get; set; } = false; // Default to false
            public int LevelToAutoUnlock { get; set; } = 1; // Default to 0
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchBox.Text.ToLower();
            engramCollectionView.Filter = engram =>
            {
                if (String.IsNullOrEmpty(searchTerm))
                    return true;  // Show all items when the search box is empty

                var engramOverride = engram as EngramOverride;
                return engramOverride?.EngramClassName.ToLower().Contains(searchTerm) == true;
            };
            engramCollectionView.Refresh();
        }

        public class EngramConfig
        {
            public string EngramClassName { get; set; }
            public bool EngramHidden { get; set; }
            public int EngramPointsCost { get; set; }
            public int EngramLevelRequirement { get; set; }
            public bool RemoveEngramPreReq { get; set; }
            public bool AutoUnlock { get; set; }
        }


        public void LoadStackSizeOverrides()
        {
            if (File.Exists(stackOverridesPath))
            {
                string jsonContent = File.ReadAllText(stackOverridesPath);
                List<StackSizeOverride> items = JsonConvert.DeserializeObject<List<StackSizeOverride>>(jsonContent);
                StackSizeOverrides = new ObservableCollection<StackSizeOverride>(items);
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            string iniFilePath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            UpdateGameIni(iniFilePath, ViewModel.StackSizeOverrides);
        }
        private void UpdateGameIni(string iniFilePath, ObservableCollection<StackSizeOverride> stackSizeOverrides)
        {
            // Read all lines of the Game.ini file into memory
            List<string> iniLines = File.ReadAllLines(iniFilePath).ToList();

            // Find the index of the header section
            int headerIndex = iniLines.FindIndex(line => line.Trim().Equals("[/script/shootergame.shootergamemode]", StringComparison.OrdinalIgnoreCase));

            // If the section header doesn't exist, add it to the end of the file
            if (headerIndex == -1)
            {
                iniLines.Add("[/script/shootergame.shootergamemode]");
                headerIndex = iniLines.Count - 1;
            }

            // Go through each override and update the ini file
            foreach (var overrideItem in stackSizeOverrides)
            {
                // Construct the line to look for or add
                string overrideLine = $"ConfigOverrideItemMaxQuantity=(ItemClassString=\"{overrideItem.ItemClassString}\",Quantity=(MaxItemQuantity={overrideItem.MaxItemQuantity}, bIgnoreMultiplier={overrideItem.IgnoreMultiplier.ToString().ToLowerInvariant()}))";

                // Check if this line already exists based on the ItemClassString
                int existingLineIndex = iniLines.FindIndex(headerIndex, line => line.Contains($"ItemClassString=\"{overrideItem.ItemClassString}\""));

                if (existingLineIndex != -1)
                {
                    // Update existing line
                    iniLines[existingLineIndex] = overrideLine;
                }
                else
                {
                    // Add new line under the header
                    iniLines.Insert(headerIndex + 1, overrideLine);
                }
            }

            // Write the updated lines back to the Game.ini file
            File.WriteAllLines(iniFilePath, iniLines);
        }


        public void LoadStackSizeOverridesConfigs()
        {
            string iniFilePath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
            ViewModel.LoadStackSizeOverrides(iniFilePath);
            dgStackSizeOverrides.ItemsSource = ViewModel.StackSizeOverrides;
        }


        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            // Create a new item with blank defaults
            var newItem = new StackSizeOverride
            {
                ItemClassString = "", // Set default or leave blank
                MaxItemQuantity = 1,  // Set default quantity
                IgnoreMultiplier = false // Set default checkbox state
            };

            // Add the new item to the ObservableCollection bound to the DataGrid
            ViewModel.StackSizeOverrides.Add(newItem);
        }

        private void PasteItems_Click(object sender, RoutedEventArgs e)
        {
            // Get text from clipboard
            var text = Clipboard.GetText();

            // Call method to process and add items
            ProcessAndAddItems(text);
        }
        // INI format pattern
        string iniPattern = @"ConfigOverrideItemMaxQuantity=\(ItemClassString=""(.+?)"",Quantity=\(MaxItemQuantity=(\d+),bIgnoreMultiplier=(true|false)\)\)";
        // JSON format pattern
        string jsonPattern = @"{\s*""itemClassString"":\s*""(.+?)"",\s*""maxItemQuantity"":\s*(\d+),\s*""ignoreMultiplier"":\s*(true|false)\s*},?";

        private void ProcessAndAddItems(string pastedText)
        {
            // Define the regex objects using the patterns
            Regex iniRegex = new Regex(iniPattern);
            Regex jsonRegex = new Regex(jsonPattern);

            // Split the pasted text by new lines and iterate over each line
            var lines = pastedText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // Check and process the INI format
                if (iniRegex.IsMatch(line))
                {
                    var match = iniRegex.Match(line);
                    var newItem = new StackSizeOverride
                    {
                        ItemClassString = match.Groups[1].Value,
                        MaxItemQuantity = int.Parse(match.Groups[2].Value),
                        IgnoreMultiplier = bool.Parse(match.Groups[3].Value)
                    };
                    ViewModel.StackSizeOverrides.Add(newItem);
                }
                // Check and process the JSON format
                else if (jsonRegex.IsMatch(line))
                {
                    var match = jsonRegex.Match(line);
                    var newItem = new StackSizeOverride
                    {
                        ItemClassString = match.Groups[1].Value,
                        MaxItemQuantity = int.Parse(match.Groups[2].Value),
                        IgnoreMultiplier = bool.Parse(match.Groups[3].Value)
                    };
                    ViewModel.StackSizeOverrides.Add(newItem);
                }
            }

            // Refresh the DataGrid
            RefreshDataGrid();
        }
        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("RemoveRow_Click invoked.");

            // Get the Button that was clicked
            var button = sender as System.Windows.Controls.Button; // Use the standard Button class unless you have a specific reason not to
            if (button == null)
            {
                Debug.WriteLine("The clicked object is not a button.");
                return;
            }

            Debug.WriteLine("Button was clicked.");

            // Get the StackSizeOverride item related to the button clicked
            StackSizeOverride itemToRemove = button.DataContext as StackSizeOverride;
            if (itemToRemove == null)
            {
                Debug.WriteLine("DataContext is not of type StackSizeOverride.");
                return;
            }

            Debug.WriteLine($"Removing item: {itemToRemove.ItemClassString}");

            // Remove the item from the ObservableCollection
            ViewModel.StackSizeOverrides.Remove(itemToRemove);

            // Refresh the DataGrid if necessary
            dgStackSizeOverrides.ItemsSource = null;
            dgStackSizeOverrides.ItemsSource = ViewModel.StackSizeOverrides;

            // Output the current count of items
            Debug.WriteLine($"Current item count: {ViewModel.StackSizeOverrides.Count}");

            string iniFilePath = Path.Combine(ViewModel.CurrentServerConfig.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");

            // Create the config string to look for in the INI file
            string configToRemove = $"ConfigOverrideItemMaxQuantity=(ItemClassString=\"{itemToRemove.ItemClassString}\"";

            // Call RemoveConfigEntries with the appropriate arguments
            RemoveConfigEntries(iniFilePath, configToRemove);
        }

        private void RemoveConfigEntries(string iniFilePath, string configStartsWith)
        {
            // Read the existing INI file into memory
            var iniFileLines = File.ReadAllLines(iniFilePath).ToList();

            // Find and remove all lines that start with the specified config string
            iniFileLines.RemoveAll(line => line.Trim().StartsWith(configStartsWith));

            // Write the updated INI file back to disk
            File.WriteAllLines(iniFilePath, iniFileLines);

            Debug.WriteLine("Config entries removed.");
        }



        private void UpdateIniFileWithOverrides(string iniFilePath, List<StackSizeOverride> overrides)
        {
            // Read the existing ini file content
            var iniFileContents = File.ReadAllText(iniFilePath);
            var sb = new StringBuilder(iniFileContents);

            // Define the section where the overrides should be updated
            string sectionHeader = "[/script/shootergame.shootergamemode]";

            // Find the index of the section header
            int sectionStart = iniFileContents.IndexOf(sectionHeader);
            if (sectionStart == -1)
            {
                // If the section is not found, append it to the end of the file
                sb.AppendLine(sectionHeader);
                sectionStart = sb.Length;
            }
            else
            {
                // If the section is found, find the end of the section
                int sectionEnd = iniFileContents.IndexOf("\n[", sectionStart + 1);
                if (sectionEnd == -1) sectionEnd = iniFileContents.Length;

                // Clear existing overrides from this section
                sb.Remove(sectionStart, sectionEnd - sectionStart);

                // Insert the section header back after clearing the section
                sb.Insert(sectionStart, sectionHeader + "\n");
            }

            // Serialize the overrides and append them to the section
            foreach (var overrideItem in overrides)
            {
                string overrideEntry = $"ConfigOverrideItemMaxQuantity=(ItemClassString=\"{overrideItem.ItemClassString}\",Quantity=(MaxItemQuantity={overrideItem.MaxItemQuantity},bIgnoreMultiplier={overrideItem.IgnoreMultiplier.ToString().ToLower()}))\n";
                sb.Insert(sectionStart, overrideEntry);
            }

            // Write the updated contents back to the ini file
            File.WriteAllText(iniFilePath, sb.ToString());
        }




        // Helper method to find a parent of a given control
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null) return parent;
            return FindParent<T>(parentObject);
        }


        private StackSizeOverride ProcessIniLine(string iniLine)
        {
            // Define the regex pattern for the INI format
            string pattern = @"ConfigOverrideItemMaxQuantity=\(ItemClassString=""(.+?)"",Quantity=\(MaxItemQuantity=(\d+),bIgnoreMultiplier=(true|false)\)\)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(iniLine);

            if (match.Success)
            {
                // Extract the values using the named groups
                string extractedItemClassString = match.Groups[1].Value;
                int extractedMaxItemQuantity = int.Parse(match.Groups[2].Value);
                bool extractedIgnoreMultiplier = bool.Parse(match.Groups[3].Value);

                // Create a new StackSizeOverride using the extracted values
                return new StackSizeOverride
                {
                    ItemClassString = extractedItemClassString,
                    MaxItemQuantity = extractedMaxItemQuantity,
                    IgnoreMultiplier = extractedIgnoreMultiplier
                };
            }
            else
            {
                // Handle the case where the line does not match the pattern
                throw new ArgumentException("The provided INI line does not match the expected format.", nameof(iniLine));
            }
        }


        private StackSizeOverride ProcessJsonLine(string jsonLine)
        {
            // Deserialize the JSON line into a StackSizeOverride object
            return JsonConvert.DeserializeObject<StackSizeOverride>(jsonLine);
        }

        private void RefreshDataGrid()
        {
            dgStackSizeOverrides.ItemsSource = null;
            dgStackSizeOverrides.ItemsSource = ViewModel.StackSizeOverrides;
        }


        public class StackSizeOverride
        {
            public string ItemClassString { get; set; }
            public int MaxItemQuantity { get; set; }
            public bool IgnoreMultiplier { get; set; }
        }


     
        public class ServerProfile
        {
            public string ChangeNumberStatus { get; set; }
            public bool IsMapNameOverridden { get; set; }
            public string ProfileName { get; set; }
            public int? Pid { get; set; }
            public string ServerStatus { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }
            public bool IsRunning { get; set; }
            public int ChangeNumber { get; set; }
            public string ServerName { get; set; }
            public int ListenPort { get; set; } // Ports are typically integers
            public int RCONPort { get; set; }   // Ports are typically integers
            public List<string> Mods { get; set; } // Assuming Mods can be a list
            public int MaxPlayerCount { get; set; }
            public string AdminPassword { get; set; }
            public string ServerPassword { get; set; }
            public bool UseBattlEye { get; set; } // Use bool for checkboxes
            public bool ForceRespawnDinos { get; set; } // Use bool for checkboxes
            public bool PreventSpawnAnimation { get; set; }

        }

        private void OpenServerFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ConfigPageViewModel; // Replace with your actual ViewModel class name if different
            if (viewModel != null && !string.IsNullOrEmpty(viewModel.CurrentServerConfig.ServerPath))
            {
                string serverFolderPath = viewModel.CurrentServerConfig.ServerPath;

                // Open the directory in Windows Explorer
                Process.Start("explorer.exe", serverFolderPath);
            }
            else
            {
                // Optionally, show a message if the path is not set
                System.Windows.MessageBox.Show("The server path is not set.");
            }
        }
       


        private void InitializeCheckBoxStates()
        {
            var viewModel = DataContext as ConfigPageViewModel; // Replace 'ConfigPageViewModel' with your actual ViewModel class name

            if (viewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("InitializeCheckBoxStates: ViewModel is null");
                return;
            }

            var excludedMapNames = new List<string> { "TheIsland_WP" }; // Add more items as needed

            if (!string.IsNullOrWhiteSpace(viewModel.OverrideMapName) &&
                !excludedMapNames.Contains(viewModel.OverrideMapName))
            {
                
                OverrideTextBox.Visibility = Visibility.Visible;
                OverrideCheckBox.IsChecked = true;
               
            }
            else
            {
                
                OverrideTextBox.Visibility = Visibility.Collapsed;
                OverrideCheckBox.IsChecked = false;
                
            }
        }

        private void OverrideCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            
            Dispatcher.Invoke(() =>
            {
                OverrideTextBox.Visibility = Visibility.Visible;
            });
        }


        private void OverrideCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
          
            OverrideTextBox.Visibility = Visibility.Collapsed;
        }



        private void InitializeCheckBoxState()
        {

            var viewModel = DataContext as ConfigPageViewModel;

            if (viewModel != null && !string.IsNullOrWhiteSpace(viewModel.OverridePassiveMod))
            {
                
                // If there's a value, check the checkbox
                OverrideModCheckBox.IsChecked = true; // This line assumes that you have named your CheckBox as 'OverrideModCheckBox' in your XAML
            }
            else
            {
                // If there's no value, uncheck the checkbox
                OverrideModCheckBox.IsChecked = false; // Same assumption as above
            }
        }

        private void OverrideModCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            OverrideModTextBox.Visibility = Visibility.Visible; // Make sure OverrideModTextBox is the name of your TextBox
        }

        // Event handler for when the CheckBox is unchecked
        private void OverrideModCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            OverrideModTextBox.Visibility = Visibility.Collapsed; // Make sure OverrideModTextBox is the name of your TextBox
        }


        // Assuming this method gets called when MOTD changes.
        /*private void UpdateRichTextPreview(string motd)
        {

            Debug.WriteLine("Updating Rich Text Preview");
            var flowDoc = new FlowDocument();

            // The pattern includes the color start tag, the color end tag, and the newline escape sequence
            var pattern = @"(<RichColor Color=""[0-9,. ]+"">|</>|\\n)";
            var segments = Regex.Split(motd, pattern);
            

            Color currentColor = Colors.Black; // Default color
            Paragraph paragraph = new Paragraph();

            foreach (var segment in segments)
            {
                if (segment.StartsWith("<RichColor"))
                {
                    currentColor = ExtractColorFromTag(segment);
                }
                else if (segment.Equals("</>")) // Check if the segment is the color end tag
                {
                    // Do nothing, just here to prevent adding the closing tag as text
                }
                else if (segment.Equals("\\n")) // Check if the segment is the newline escape sequence
                {
                    // Add a new line to the paragraph
                    paragraph.Inlines.Add(new LineBreak());
                }
                else
                {
                    // This segment is the text that should have the current color
                    Run run = new Run(segment)
                    {
                        Foreground = new SolidColorBrush(currentColor)
                    };
                    paragraph.Inlines.Add(run);
                }
            }

            flowDoc.Blocks.Add(paragraph);
            richTextPreview.Document = flowDoc;
            Debug.WriteLine("Rich Text Preview Updated");
        }*/
        private void UpdateRichTextPreview(string motd)
        {
            Debug.WriteLine("Updating Rich Text Preview");
            FlowDocument flowDoc;
            // Check if 'motd' is null or empty before proceeding
            if (string.IsNullOrEmpty(motd))
            {
                // Initialize flowDoc here with a default message
                flowDoc = new FlowDocument();
                flowDoc.Blocks.Add(new Paragraph(new Run("No message of the day provided.")));
                richTextPreview.Document = flowDoc;
                Debug.WriteLine("Rich Text Preview Updated with default message.");
                return; // Exit the method as there's nothing to process.
            }

            // If 'motd' is not null or empty, initialize flowDoc here
            flowDoc = new FlowDocument();

            // The pattern includes the color start tag, the color end tag, and the newline escape sequence
            var pattern = @"(<RichColor Color=""[0-9,. ]+"">|</>|\\n)";
            var segments = Regex.Split(motd, pattern);

            Color currentColor = Colors.Black; // Default color
            Paragraph paragraph = new Paragraph();

            foreach (var segment in segments)
            {
                if (segment.StartsWith("<RichColor"))
                {
                    currentColor = ExtractColorFromTag(segment);
                }
                else if (segment.Equals("</>")) // Check if the segment is the color end tag
                {
                    // Do nothing, just here to prevent adding the closing tag as text
                }
                else if (segment.Equals("\\n")) // Check if the segment is the newline escape sequence
                {
                    // Add a new line to the paragraph
                    paragraph.Inlines.Add(new LineBreak());
                }
                else
                {
                    // This segment is the text that should have the current color
                    Run run = new Run(segment)
                    {
                        Foreground = new SolidColorBrush(currentColor)
                    };
                    paragraph.Inlines.Add(run);
                }
            }

            flowDoc.Blocks.Add(paragraph);
            richTextPreview.Document = flowDoc;
            Debug.WriteLine("Rich Text Preview Updated");
        }

        private IEnumerable<Paragraph> ParseMOTDToParagraphs(string motd)
        {
            List<Paragraph> paragraphs = new List<Paragraph>();
            Color currentColor = Colors.Black; // Default color

            int i = 0;
            while (i < motd.Length)
            {
                Debug.WriteLine($"Processing character at index {i}: {motd[i]}");

                if (motd[i] == '<' && i + 1 < motd.Length && motd[i + 1] == 'R') // Start of color tag
                {
                    Debug.WriteLine("Start of color tag detected.");

                    // Finish the current text block
                    Paragraph paragraph = new Paragraph();
                    paragraph.Foreground = new SolidColorBrush(currentColor);
                    paragraphs.Add(paragraph);

                    int tagEnd = motd.IndexOf('>', i);
                    if (tagEnd != -1)
                    {
                        string colorTag = motd.Substring(i, tagEnd - i + 1);
                        currentColor = ExtractColorFromTag(colorTag); // Extract color

                        Debug.WriteLine($"Color tag: {colorTag}");
                        Debug.WriteLine($"New color: {currentColor}");

                        i = tagEnd + 1; // Move index to after the color tag
                        Debug.WriteLine($"Moved index to {i}");
                    }
                }
                else
                {
                    // Find the end of the text or color tag
                    int nextTagStart = motd.IndexOf('<', i);
                    int endIndex = (nextTagStart != -1) ? nextTagStart : motd.Length;

                    string text = motd.Substring(i, endIndex - i);
                    Paragraph paragraph = new Paragraph(new Run(text));
                    paragraph.Foreground = new SolidColorBrush(currentColor);
                    paragraphs.Add(paragraph);

                    Debug.WriteLine($"Text: {text}");
                    Debug.WriteLine($"Color: {currentColor}");

                    i = endIndex;
                    Debug.WriteLine($"Moved index to {i}");
                }
            }

            return paragraphs;
        }



        private Color ExtractColorFromTag(string colorTag)
        {
            Debug.WriteLine($"ExtractColorFromTag called with: {colorTag}");

            const string prefix = "<RichColor Color=\"";
            const string suffix = "\">";

            int start = colorTag.IndexOf(prefix);
            if (start == -1)
            {
                Debug.WriteLine("Prefix not found.");
                return Colors.Black; // Prefix not found
            }
            start += prefix.Length;

            int end = colorTag.IndexOf(suffix, start);
            if (end == -1)
            {
                Debug.WriteLine("Suffix not found.");
                return Colors.Black; // Suffix not found or not after prefix
            }

            string colorValues = colorTag.Substring(start, end - start);
            string[] rgba = colorValues.Split(',');

            Debug.WriteLine($"Extracted color values: {colorValues}");

            if (rgba.Length == 4 &&
                float.TryParse(rgba[0], out float r) &&
                float.TryParse(rgba[1], out float g) &&
                float.TryParse(rgba[2], out float b) &&
                float.TryParse(rgba[3], out float a))
            {
                Color color = Color.FromArgb((byte)(a * 255), (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
                Debug.WriteLine($"Parsed color: {color}");
                return color;
            }

            Debug.WriteLine("Failed to parse color.");
            return Colors.Black; // Default color if parsing fails
        }

        private void RestoreBackUpButton_Click(object sender, RoutedEventArgs e)
        {
            // Assuming ServerConfig is a class that represents your server configuration
            ServerConfig serverConfig = new ServerConfig
            {
                ProfileName = ViewModel.CurrentServerConfig.ProfileName, // Replace with actual data
                ServerPath = ViewModel.CurrentServerConfig.ServerPath,
                MapName = ViewModel.CurrentServerConfig.MapName,
            };

            SaveServerConfigToJson(serverConfig);
        }

        private void SaveServerConfigToJson(ServerConfig serverConfig) 
        {
            Debug.WriteLine($"Saving ServerConfig with MapName: {serverConfig.MapName ?? "null"}");
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appNameFolder = Path.Combine(appDataFolderPath, "Ark Ascended Manager");
            string jsonFileName = "RestoreBackUpDataStruc.json";
            string jsonFilePath = Path.Combine(appNameFolder, jsonFileName);

            // Ensure the directory exists
            Directory.CreateDirectory(appNameFolder);

            // Serialize the serverConfig object to JSON
            string json = JsonConvert.SerializeObject(serverConfig);

            // Save the JSON to a file
            File.WriteAllText(jsonFilePath, json);

            _navigationService.Navigate(typeof(RestorePage));
        }


        public void OnNavigatedTo(object parameter)
        {
            
            Console.WriteLine("OnNavigatedTo called in ConfigPage.");
            if (parameter is ServerConfig serverConfig)
            {
                Console.WriteLine($"ServerConfig received: {serverConfig.ProfileName}");
                ViewModel.LoadConfig(serverConfig);
                UpdateRichTextPreview(ViewModel.MOTD);


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
            string searchQuery = SearchBoxGameIni.Text.ToLower();
            // Search through the expanders and their content
            Expander result = FindExpanderWithContent(Gameini, searchQuery);
            FrameworkElement resultControl = FindControlWithText(Gameini, searchQuery);

            if (resultControl != null)
            {
                // If a control was found, expand its parent Expander
                if (result != null)
                {
                    result.IsExpanded = true;
                }
                HighlightControl(resultControl); // This is safe to call now that we've checked resultControl is not null
            }
            else
            {
                System.Windows.MessageBox.Show("No matching settings found.");
            }
        }

        private void HighlightControl(FrameworkElement control)
        {
            if (control == null) return; // Ensure control is not null

            switch (control)
            {
                case System.Windows.Controls.TextBox textBox:
                    textBox.Focus();
                    textBox.SelectAll();
                    break;
                case Label label:
                    var currentLabelBackground = label.Background ?? Brushes.Transparent; // Handle a potentially null background
                    label.Background = new SolidColorBrush(Colors.Orange);
                    var labelTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                    labelTimer.Tick += (sender, args) =>
                    {
                        labelTimer.Stop();
                        label.Background = currentLabelBackground;
                    };
                    labelTimer.Start();
                    break;
                case System.Windows.Controls.TextBlock textBlock:
                    var currentTextBlockBackground = textBlock.Background ?? Brushes.Transparent; // Handle a potentially null background
                    textBlock.Background = new SolidColorBrush(Colors.Orange);
                    var textBlockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                    textBlockTimer.Tick += (sender, args) =>
                    {
                        textBlockTimer.Stop();
                        textBlock.Background = currentTextBlockBackground;
                    };
                    textBlockTimer.Start();
                    break;
                    // Add cases for other control types as necessary
            }

            // Bring the control into view if it's inside a ScrollViewer
            control.BringIntoView();
        }




        private Expander FindExpanderWithContent(DependencyObject parent, string searchQuery)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // First, check if the child is an Expander
                if (child is Expander expander)
                {
                    // Check the header of the Expander
                    if (expander.Header is string header && header.ToLower().Contains(searchQuery))
                    {
                        return expander;
                    }
                    // Check the content of the Expander if it is expanded or not
                    if (expander.IsExpanded || expander.Content is not null)
                    {
                        // Search through the content of the Expander
                        var contentAsDependencyObject = expander.Content as DependencyObject;
                        if (contentAsDependencyObject != null)
                        {
                            var foundInContent = FindControlWithText(contentAsDependencyObject, searchQuery);
                            if (foundInContent != null)
                            {
                                // If found, return this Expander
                                return expander;
                            }
                        }
                    }
                }
                // Recursive search inside the child controls
                var foundExpander = FindExpanderWithContent(child, searchQuery);
                if (foundExpander != null)
                {
                    return foundExpander;
                }
            }
            return null;
        }

        private FrameworkElement FindControlWithText(DependencyObject parent, string searchQuery)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Check various control types that might contain text
                if (child is System.Windows.Controls.TextBlock textBlock && textBlock.Text.ToLower().Contains(searchQuery))
                {
                    return textBlock;
                }
                else if (child is System.Windows.Controls.TextBox textBox && textBox.Text.ToLower().Contains(searchQuery))
                {
                    return textBox;
                }
                else if (child is Label label && label.Content is string labelText && labelText.ToLower().Contains(searchQuery))
                {
                    return label;
                }
                else if (child is ContentControl contentControl && contentControl.Content is string content && content.ToLower().Contains(searchQuery))
                {
                    return contentControl;
                }

                // Otherwise, continue recursive search
                var result = FindControlWithText(child, searchQuery);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }







        public void OnNavigatedFrom()
        {
            // If there's anything you need to do when navigating away from this page, do it here.
        }

        // The ViewModel property is publicly accessible and set in the constructor.
        public ConfigPageViewModel ViewModel { get; }

























    }

}