using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Ark_Ascended_Manager.Services;
using Ark_Ascended_Manager.ViewModels.Windows;
using Ark_Ascended_Manager.Views.Pages;
using Newtonsoft.Json;
using Wpf.Ui.Controls;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using Icon = System.Drawing.Icon;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Ark_Ascended_Manager.Views.Windows
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindowViewModel ViewModel { get; }
        private readonly INavigationService _navigationService;
        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ISnackbarService snackbarService,
            IContentDialogService contentDialogService
        )
        {
            Wpf.Ui.Appearance.Watcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            InitializeComponent();

            navigationService.SetNavigationControl(NavigationView);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetContentPresenter(RootContentDialog);

            NavigationView.SetServiceProvider(serviceProvider);

            InitializeNotifyIcon();

            // Count online servers and show notification
            int onlineServersCount = CountOnlineServers();
            NotifyAAMStarted(onlineServersCount);
        }

        private void InitializeNotifyIcon()
        {
            var resourceUri = new Uri("pack://application:,,,/Assets/AAM_Icon_exe.ico");

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new Icon(System.Windows.Application.GetResourceStream(resourceUri).Stream),
                Visible = true,
                Text = "Ark Ascended Manager"
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            var contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add("Open", null, (s, e) => ShowWindow());
            contextMenuStrip.Items.Add("Settings", null, (s, e) => OpenSettingsPage());
            contextMenuStrip.Items.Add("Exit", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowWindow();
        }

        private void OpenSettingsPage()
        {
            Debug.WriteLine("_navigationService is null: " + (_navigationService == null));
            if (_navigationService == null)
            {
                throw new InvalidOperationException("_navigationService is not initialized.");
            }

            ShowWindow();
            _navigationService.Navigate(typeof(SettingsPage));
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Cancel the close event
            Hide();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Cancel the close event
            Hide();
        }

        private void ShowNotification(string title, string message)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }

        public void NotifyAAMStarted(int serverCount)
        {
            string message = $"AAM has started: {serverCount} server(s) detected online.";
            ShowNotification("Ark Ascended Manager", message);
        }

        private int CountOnlineServers()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string jsonFilePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

            if (!File.Exists(jsonFilePath))
            {
                // Handle the case where the file does not exist
                return 0;
            }

            var serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(File.ReadAllText(jsonFilePath));
            int onlineServersCount = serverConfigs.Count(server => server.ServerStatus == "Online");
            return onlineServersCount;
        }
    }

    public class ServerConfig
    {
        public string ProfileName { get; set; }
        public string ServerStatus { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        public string AppId { get; set; }
        public string ServerIP { get; set; }
        public bool IsRunning { get; set; }
        public string ServerName { get; set; }
        public int ListenPort { get; set; }
        public int RCONPort { get; set; }
        public List<string> Mods { get; set; }
        public int MaxPlayerCount { get; set; }
        public string AdminPassword { get; set; }
        public string ServerIcon { get; set; }
        public string ServerPassword { get; set; }
        public bool UseBattlEye { get; set; }
        public bool ForceRespawnDinos { get; set; }
        public bool PreventSpawnAnimation { get; set; }
        public int ChangeNumber { get; set; }
        public string ChangeNumberStatus { get; set; }
    }
}
