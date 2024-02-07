// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.IO;
using System.Windows;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Reflection;
using System.Diagnostics;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public ObservableCollection<string> UploadedFiles { get; private set; } = new ObservableCollection<string>();
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            DataContext = this;
            this.uploadedFilesList.ItemsSource = UploadedFiles;
            CheckForUpdatesAsync();
            LoadUploadedFilesList();

           
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

        private void LoadUploadedFilesList()
        {
            UploadedFiles.Clear();
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ark ascended manager", "Data", "Engrams");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            else
            {
                foreach (var file in Directory.GetFiles(folderPath))
                {
                    UploadedFiles.Add(Path.GetFileName(file));
                }
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
        private async void UpdateApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable the button to prevent multiple clicks
            UpdateApplicationButton.IsEnabled = false;

            try
            {
                // URL of the zip file containing the MSI installer on GitHub
                string downloadUrl = "https://github.com/RomanGod6/Ark-Ascended-Manager-Updater/releases/download/{tag}/YourAppInstaller.zip";

                // Start the update process
                await DownloadAndInstallUpdateAsync(downloadUrl);
            }
            catch (Exception ex)
            {
                // Re-enable the button in case of an error
                UpdateApplicationButton.IsEnabled = true;

                // Handle any exceptions that occur during the process
                System.Windows.MessageBox.Show($"Failed to update the application: {ex.Message}", "Update Failed", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task DownloadAndInstallUpdateAsync(string downloadUrl)
        {
            using (var client = new HttpClient())
            {
                // Download the zip file
                var zipBytes = await client.GetByteArrayAsync(downloadUrl);

                // Save the zip file to a temporary location
                var tempZipPath = Path.GetTempFileName();
                await File.WriteAllBytesAsync(tempZipPath, zipBytes);

                // Extract the MSI from the zip
                string tempExtractionPath = Path.GetTempPath() + "\\YourAppInstaller";
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractionPath);

                // Find the MSI file in the extracted files
                var msiFilePath = Directory.GetFiles(tempExtractionPath, "*.msi").FirstOrDefault();

                if (msiFilePath != null)
                {
                    // Start the MSI installation process
                    Process.Start("msiexec", $"/i \"{msiFilePath}\"");
                }
                else
                {
                    throw new FileNotFoundException("MSI file not found in the downloaded zip.");
                }
            }
        }


        private async Task CheckForUpdatesAsync()
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var updateCheckUrl = "https://api.github.com/repos/RomanGod6/Ark-Ascended-Manager-Updater/releases/latest";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Ark-Ascended-Manager-Update-Check");

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
                        System.Windows.MessageBox.Show("A new update is available!", "Update Available", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                        // You would typically provide a way for the user to download the update here
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("You have the latest version installed.", "No Updates", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to check for updates.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        
        

    }
}
