using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System;
using System.Windows;
using System.Windows.Forms; // Add the WinForms namespace for FolderBrowserDialog

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ServersPage : INavigableView<ServersViewModel>
    {
        private readonly INavigationService _navigationService;

        // Inject INavigationService through the constructor
        public ServersPage(ServersViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            DataContext = ViewModel;

            // Assuming LoadServerConfigs is properly implemented in ServersViewModel
            ViewModel.LoadServerConfigs();
        }

        // Event handler for your button click
        private void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            // Use the navigation service to navigate to CreateServersPage
            _navigationService.Navigate(typeof(CreateServersPage));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OnNavigatedTo(); // This should call LoadServerConfigs internally
        }

        private void SyncServers_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the SyncServersPage
            var syncServersPage = new SyncConfigPage();
            this.NavigationService.Navigate(syncServersPage);
        }
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            // Assuming ServersViewModel is your data context
            var viewModel = DataContext as ServersViewModel;
            if (viewModel != null && viewModel.CurrentServerInfo != null)
            {
                bool isRunning = viewModel.IsServerRunning(viewModel.CurrentServerInfo);
                System.Windows.MessageBox.Show($"Server is running: {isRunning}");
            }
            else
            {
                System.Windows.MessageBox.Show("CurrentServerInfo is not set.");
            }
        }



        private void ImportServers_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.Navigate(typeof(ImportServersPage));
        }

        // ViewModel property
        public ServersViewModel ViewModel { get; }
    }
}
