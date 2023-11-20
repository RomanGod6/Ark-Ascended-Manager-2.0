
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class RconPanelPage : INavigableView<RconPanelViewModel>
    {
        public RconPanelViewModel ViewModel { get; }

        public RconPanelPage(RconPanelViewModel viewModel)
        {
            ViewModel = viewModel;
            

            InitializeComponent();
            DataContext = new RconPanelViewModel();
        }
    }
}
