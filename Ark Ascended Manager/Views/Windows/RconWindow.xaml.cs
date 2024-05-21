using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Ark_Ascended_Manager.ViewModels.Windows;

namespace Ark_Ascended_Manager.Views.Windows
{
    public partial class RconWindow : Window
    {
        public RconWindow()
        {
            InitializeComponent();
        }

        public void AddRconTab(RconViewModel rconViewModel)
        {
            var rconTabContent = new RconTabContent
            {
                DataContext = rconViewModel
            };

            Debug.WriteLine($"Adding Tab for Server: {rconViewModel.ServerName}");
            Debug.WriteLine($"RconTabContent DataContext: {rconTabContent.DataContext}");

            var tabItem = new TabItem
            {
                Header = rconViewModel.ServerName,
                Content = rconTabContent
            };

            RconTabControl.Items.Add(tabItem);

            Debug.WriteLine($"Tab count after adding: {RconTabControl.Items.Count}");
        }

        public int GetTabCount()
        {
            return RconTabControl.Items.Count;
        }
    }
}
