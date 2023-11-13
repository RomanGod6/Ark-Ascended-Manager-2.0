using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System; // Ensure System namespace is included for ArgumentNullException
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;



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
        }

        // Implement the interface member of INavigableView to handle navigation with a parameter.
        public void OnNavigatedTo(object parameter)
        {
            // If you're passing the ServerConfig as a parameter, this is where you would handle it.
            // Check if the ViewModel has a method named LoadConfig and call it with the parameter.
            if (parameter is ServerConfig serverConfig)
            {
                ViewModel.LoadConfig(serverConfig); // Make sure this method exists in your ViewModel
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
