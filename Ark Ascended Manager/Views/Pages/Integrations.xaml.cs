
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class IntegrationsPage : INavigableView<IntegrationsViewModel>
    {
        public IntegrationsViewModel ViewModel { get; }

        public IntegrationsPage(IntegrationsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
