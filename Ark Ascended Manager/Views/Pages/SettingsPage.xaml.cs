using System.Windows.Controls;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;
using System.Windows.Media;
using System.Threading.Tasks;
using System;
using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Pages;
using System.IO;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public ObservableCollection<string> UploadedFiles { get; private set; } = new ObservableCollection<string>();
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel)); // Ensure ViewModel is not null
            InitializeComponent();
            DataContext = this; // Set DataContext to ViewModel
            this.uploadedFilesList.ItemsSource = UploadedFiles;
            Loaded += async (s, e) => await CheckAndUpdateVersionStatusAsync();
            Loaded += SettingsPage_Loaded;
            CheckForUpdatesAsync();
            LoadUploadedFilesList();
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadReleaseNotes();
        }

        private void UploadJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json";
            if (openFileDialog.ShowDialog() == true)
            {
                string destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ark ascended manager", "Data", "Engrams", Path.GetFileName(openFileDialog.FileName));
                File.Copy(openFileDialog.FileName, destinationPath, true);
                LoadUploadedFilesList();
            }
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedComboBoxItem = e.AddedItems[0] as ComboBoxItem;
                if (selectedComboBoxItem != null)
                {
                    string selectedLanguage = selectedComboBoxItem.Tag.ToString();
                    ViewModel.GlobalSettings.Language = selectedLanguage;
                    ViewModel.ApplyLanguage(selectedLanguage);
                    ViewModel.SaveSettings();
                }
            }
        }

        private void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string arkAscendedManagerFolder = Path.Combine(appDataFolder, "Ark Ascended Manager");

            // Ensure the directory exists before trying to open it
            if (!Directory.Exists(arkAscendedManagerFolder))
            {
                // Optionally, create the directory if it doesn't exist, or inform the user
                // Directory.CreateDirectory(arkAscendedManagerFolder); // To create the directory
                System.Windows.MessageBox.Show("The Ark Ascended Manager folder does not exist in AppData.");
                return;
            }

            // Open the directory in Windows Explorer
            Process.Start("explorer.exe", arkAscendedManagerFolder);
        }

        private void LoadUploadedFilesList()
        {
            UploadedFiles.Clear();
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ark ascended manager", "Data", "Engrams");
            try
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                foreach (var file in Directory.GetFiles(folderPath))
                {
                    UploadedFiles.Add(Path.GetFileName(file));
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (log it, show message to the user, etc.)
                Debug.WriteLine($"Error loading uploaded files list: {ex.Message}");
                System.Windows.MessageBox.Show($"An error occurred while accessing the Engrams directory: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenIssueReportForm_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to IssueReportForm
            this.NavigationService.Navigate(new IssueReportForm());
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the path to the AppData folder
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // Construct the full path to your JSON file
                string jsonFilePath = Path.Combine(appDataFolder, "ark ascended manager", "servers.json");

                // Check if the JSON file exists
                if (File.Exists(jsonFilePath))
                {
                    // Create an instance of your ServerUpdateService and read the JSON
                    var serverUpdateService = new ServerUpdateService(jsonFilePath);
                    await serverUpdateService.CheckAndUpdateServersOnStartup();
                }
                else
                {
                    // Handle the case where the JSON file doesn't exist
                    System.Windows.MessageBox.Show("The 'servers.json' file does not exist in the 'ark ascended manager' folder.", "File Not Found", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during file access or service execution
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private const string GitHubApiLatestReleaseUrl = "https://api.github.com/repos/RomanGod6/Ark-Ascended-Manager-Updater/releases/latest";

        private async Task<string> GetLatestVersionTagAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "ArkAscendedManagerClientApplication");

                var response = await client.GetAsync(GitHubApiLatestReleaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    dynamic latestRelease = JObject.Parse(jsonString);
                    return latestRelease.tag_name;
                }
                else
                {
                    // Handle the case where the API call fails
                    throw new InvalidOperationException("Could not fetch latest version tag from GitHub.");
                }
            }
        }

        private async void UpdateApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the button to prevent multiple clicks
            UpdateApplicationButton.IsEnabled = false;

            try
            {
                // Get the latest version tag from GitHub
                string versionTag = await GetLatestVersionTagAsync();
                if (!string.IsNullOrEmpty(versionTag))
                {
                    // If a new version is available, download and install the update
                    await DownloadAndInstallUpdateAsync(versionTag.TrimStart('v'));
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., network issues, parsing errors)
                System.Windows.MessageBox.Show($"Error checking for updates: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable the button after the process is complete or has failed
                UpdateApplicationButton.IsEnabled = true;
            }
        }

        private async Task CheckAndUpdateVersionStatusAsync()
        {
            string latestVersionTag = await GetLatestVersionTagAsync(); // Should return something like "v2.4.0"
            string currentFullVersion = ViewModel.AppVersion; // Might return something like "Ark Ascended Manager - 2.4.0.0"

            // Extract just the version part of the string
            string currentVersionString = currentFullVersion.Split('-').Last().Trim();

            // Remove the 'v' prefix from the GitHub tag and ensure it's a valid version string
            latestVersionTag = latestVersionTag.TrimStart('v');
            if (Version.TryParse(latestVersionTag, out Version latestVersion) &&
                Version.TryParse(currentVersionString, out Version currentVersion))
            {
                if (latestVersion > currentVersion)
                {
                    VersionStatusTextBlock.Text = $"Update available - New version: {latestVersion}";
                    VersionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    VersionStatusTextBlock.Text = "Up to date";
                    VersionStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
            }
            else
            {
                // Handle invalid version string format
                Debug.WriteLine("Invalid version string format.");
            }
        }

        private async Task DownloadAndInstallUpdateAsync(string versionTag)
        {
            string downloadUrl = $"https://github.com/RomanGod6/Ark-Ascended-Manager-Updater/archive/refs/tags/v{versionTag}.zip";
            string tempZipPath = Path.GetTempFileName();
            string extractionPath = Path.Combine(Path.GetTempPath(), "ArkAscendedManagerUpdate");
            string msiFileName = "ArkAscendedManager.msi";

            using (var client = new HttpClient())
            {
                // Download the ZIP file
                try
                {
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempZipPath, fileBytes);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error downloading update: {ex.Message}", "Download Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Extract the ZIP file
                try
                {
                    if (Directory.Exists(extractionPath))
                    {
                        Directory.Delete(extractionPath, true); // Ensures the directory is clean before extracting
                    }
                    Directory.CreateDirectory(extractionPath);
                    ZipFile.ExtractToDirectory(tempZipPath, extractionPath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error extracting update: {ex.Message}", "Extraction Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Find the MSI file within the extracted directory
                string msiFilePath = Directory.EnumerateFiles(extractionPath, msiFileName, SearchOption.AllDirectories).FirstOrDefault();

                if (!string.IsNullOrEmpty(msiFilePath))
                {
                    // Correctly launch the MSI installer using msiexec
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "msiexec",
                        Arguments = $"/i \"{msiFilePath}\"",
                        UseShellExecute = true,
                        Verb = "runas" // Ensure the installer runs with admin privileges
                    };

                    try
                    {
                        Process process = Process.Start(startInfo);

                        // You may want to close your application here
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to start installer: {ex.Message}", "Installation Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Installer file not found after extraction.", "Update Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var updateCheckUrl = "https://api.github.com/repos/RomanGod6/Ark-Ascended-Manager-Updater/releases/latest";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "ArkAscendedManager-Update-Check");

                var response = await httpClient.GetAsync(updateCheckUrl);
                if (response.IsSuccessStatusCode)
                {
                    var releaseInfo = await response.Content.ReadAsStringAsync();
                    var latestRelease = JObject.Parse(releaseInfo);
                    var latestVersionTag = latestRelease["tag_name"].ToString();

                    // Assuming the tag name is a version number prefixed with 'v', e.g., 'v1.0.0'
                    if (new Version(latestVersionTag.TrimStart('v')) > new Version(currentVersion))
                    {
                        // Notify the user that an update is available
                        Debug.WriteLine("A new update is available!", "Update Available", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                        // You would typically provide a way for the user to download the update here
                    }
                    else
                    {
                        Debug.WriteLine("You have the latest version installed.", "No Updates", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to check for updates.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
