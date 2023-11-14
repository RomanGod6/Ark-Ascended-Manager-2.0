using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System; // Ensure System namespace is included for ArgumentNullException
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;
using System.Windows.Controls;



namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ConfigPage : INavigableView<ConfigPageViewModel> // Make sure the base class is Page
    {
        // Constructor injects the ViewModel and sets it to the DataContext.
        public ConfigPage(ConfigPageViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // It's important to set the DataContext after initializing the components.
            DataContext = ViewModel;
            Console.WriteLine("Made it to the config page under the DataContext of Config Page");
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
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();

            // Assuming 'YourStackPanel' is the name of your StackPanel inside the ScrollViewer
            foreach (var child in GUSSearch.Children)
            {
                // Check if the child is a Label and if its content matches the search text
                if (child is Label label && label.Content.ToString().ToLower().Contains(searchText))
                {
                    // If a match is found, bring that Label into view
                    label.BringIntoView();
                    return;
                }
            }

            // Optionally, handle the scenario where no match is found
        }





        public void OnNavigatedFrom()
        {
            // If there's anything you need to do when navigating away from this page, do it here.
        }

        // The ViewModel property is publicly accessible and set in the constructor.
        public ConfigPageViewModel ViewModel { get; }
    }
}