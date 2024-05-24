using Ark_Ascended_Manager.ViewModels.Windows;
using System.Windows;

namespace Ark_Ascended_Manager.Views.Windows
{
    public partial class EditCommandWindow : Window
    {
        public EditCommandWindow(CommandMapping command)
        {
            InitializeComponent();
            DataContext = command;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
