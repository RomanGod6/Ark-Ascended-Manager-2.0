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
            UpdateRichTextPreview(ViewModel.MOTD);
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
        // Assuming this method gets called when MOTD changes.
        private void UpdateRichTextPreview(string motd)
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






        // Implement the interface member of INavigableView to handle navigation with a parameter.
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