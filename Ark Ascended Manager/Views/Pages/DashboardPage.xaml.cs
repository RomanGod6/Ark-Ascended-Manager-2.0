
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.Threading.Tasks;
namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
           
   



        }
      
    }
}
