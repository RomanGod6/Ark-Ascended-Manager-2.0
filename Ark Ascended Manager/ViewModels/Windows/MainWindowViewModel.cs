
using System.Collections.ObjectModel;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "Ark Ascended Manager";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "Servers",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ServerMultiple20 },
                TargetPageType = typeof(Views.Pages.ServersPage)
            },
             new NavigationViewItem()
            {
                Content = "RCON",
                Icon = new SymbolIcon { Symbol = SymbolRegular.VirtualNetwork20 },
                TargetPageType = typeof(Views.Pages.RconPanelPage)
            },
               new NavigationViewItem()
            {
                Content = "Integrations",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Power20 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
                 new NavigationViewItem()
            {
                Content = "The Forge",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Database20 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            }

        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
