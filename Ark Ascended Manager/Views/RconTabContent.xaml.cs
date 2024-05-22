using Ark_Ascended_Manager.ViewModels.Windows;
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
    }
}
