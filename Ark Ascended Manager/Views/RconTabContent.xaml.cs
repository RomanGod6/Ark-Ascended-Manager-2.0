using Ark_Ascended_Manager.ViewModels.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Ark_Ascended_Manager.Views.Windows
{
    public partial class RconTabContent : UserControl
    {
        public RconTabContent()
        {
            InitializeComponent();
            DataContext = new RconViewModel();
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null && listBox.SelectedItem != null)
            {
                var selectedCommand = listBox.SelectedItem as CommandMapping;
                if (selectedCommand != null)
                {
                    // Switch to edit mode
                    var editWindow = new EditCommandWindow(selectedCommand);
                    if (editWindow.ShowDialog() == true)
                    {
                        // Save changes
                        var viewModel = DataContext as RconViewModel;
                        viewModel.SaveCommandsCommand.Execute(null);
                    }
                }
            }
        }
    }
}
