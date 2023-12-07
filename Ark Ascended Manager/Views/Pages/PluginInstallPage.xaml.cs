
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class PluginInstallPage : INavigableView<PluginInstallPageViewModel>
    {
        public PluginInstallPageViewModel ViewModel { get; }
        public PluginInstallPage(PluginInstallPageViewModel viewModel)
        {
            InitializeComponent(); // This should be called first.
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;


        }
        public void OnNavigatedFrom()
        {
           
           DataContext = null;
        }

        



        // ViewModel property should be read-only if it's being set only in the constructor.
        
    }




}

