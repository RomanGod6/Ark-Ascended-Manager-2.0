
using System.Collections.ObjectModel;
using System.Drawing;
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
                Content = "Integrations",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Power20 },
                TargetPageType = typeof(Views.Pages.IntegrationsPage)
            },
                  new NavigationViewItem()
            {
                Content = "Mods",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Wrench20},
                TargetPageType = typeof(Views.Pages.CurseForgeModPage)
            },
                    new NavigationViewItem()
            {
                Content = "Tasks",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Timer12},
                TargetPageType = typeof(Views.Pages.CreateSchedulePage)
            },



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
