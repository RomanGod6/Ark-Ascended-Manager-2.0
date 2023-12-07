using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.IO;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class PluginsPage : INavigableView<PluginsPageViewModel>
    {
        public PluginsPageViewModel ViewModel { get; }

        public PluginsPage(PluginsPageViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            // Here, pass the existing viewModel instance instead of creating a new one.
            // Ensure that viewModel is properly instantiated with INavigationService.
            DataContext = viewModel;
        }
    }
}
