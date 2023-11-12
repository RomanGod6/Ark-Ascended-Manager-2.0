using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ConfigPage : INavigableView<ConfigPageViewModel>
    {
        public ConfigPageViewModel ViewModel { get; }

        public ConfigPage(ConfigPageViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            
            DataContext = ViewModel;
        }

        // If needed, implement the interface members of INavigableView
        public void OnNavigatedTo(object parameter)
        {
            if (parameter is ServerConfig serverConfig)
            {
                ViewModel.LoadConfig(serverConfig);
            }
        }

        public void OnNavigatedFrom() { }
    }
}
