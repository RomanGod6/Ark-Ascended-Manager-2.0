using System.Collections.ObjectModel;
using System.Drawing;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Ark_Ascended_Manager.Resources;

namespace Ark_Ascended_Manager.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly LocalizedStrings _localizedStrings = new LocalizedStrings();

        [ObservableProperty]
        private string _applicationTitle;

        public MainWindowViewModel()
        {
            ApplicationTitle = _localizedStrings.ApplicationTitle;

            MenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = _localizedStrings.Home,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                    TargetPageType = typeof(Views.Pages.DashboardPage)
                },
                new NavigationViewItem()
                {
                    Content = _localizedStrings.Servers,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.ServerMultiple20 },
                    TargetPageType = typeof(Views.Pages.ServersPage)
                },
                new NavigationViewItem()
                {
                    Content = _localizedStrings.Integrations,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Power20 },
                    TargetPageType = typeof(Views.Pages.IntegrationsPage)
                },
                new NavigationViewItem()
                {
                    Content = _localizedStrings.Mods,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Wrench20 },
                    TargetPageType = typeof(Views.Pages.CurseForgeModPage)
                },
                new NavigationViewItem()
                {
                    Content = _localizedStrings.Tasks,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Timer12 },
                    TargetPageType = typeof(Views.Pages.CreateSchedulePage)
                },
                 new NavigationViewItem()
                {
                    Content = _localizedStrings.Plugins,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.PuzzlePiece16 },
                    TargetPageType = typeof(Views.Pages.ArkPlugins)
                }
            };

            FooterMenuItems = new ObservableCollection<object>
            {
                new NavigationViewItem()
                {
                    Content = _localizedStrings.Settings,
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                    TargetPageType = typeof(Views.Pages.SettingsPage)
                }
            };

            TrayMenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem { Header = _localizedStrings.Home, Tag = "tray_home" }
            };
        }

        [ObservableProperty]
        private ObservableCollection<object> _menuItems;

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems;

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems;
    }
}
