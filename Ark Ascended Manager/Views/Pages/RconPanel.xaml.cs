
using Ark_Ascended_Manager.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Input;
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
        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ListBox listBox && listBox.Parent is ScrollViewer scrollViewer)
            {
                if (e.Delta > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();

                e.Handled = true;
            }
        }



    }
}
