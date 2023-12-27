// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.IO;
using System.Windows;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
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


    }
}
