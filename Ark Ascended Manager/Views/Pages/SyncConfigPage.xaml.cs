using System;
using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    // Ensure your page class inherits from the appropriate base class, like Page
    public partial class SyncConfigPage 
    {
        public SyncConfigPage()
        {
            InitializeComponent();
            DataContext = new SyncConfigViewModel();
        }
    }
}
