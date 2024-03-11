

using Ark_Ascended_Manager.Views.Pages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Windows.Input;
using Wpf.Ui.Controls;
using System.IO;

namespace Ark_Ascended_Manager.ViewModels.Pages

{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        public ICommand OpenIssueReportFormCommand { get; private set; }
        private bool _isInitialized = false;
        private readonly INavigationService _navigationService;
        
        public SettingsViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            
        }

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private Wpf.Ui.Appearance.ThemeType _currentTheme = Wpf.Ui.Appearance.ThemeType.Unknown;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
            LoadSettings();
        }
        private string _releaseNotes;
        public string ReleaseNotes
        {
            get => _releaseNotes;
            set
            {
                _releaseNotes = value;
                OnPropertyChanged(nameof(ReleaseNotes));
            }
        }
        private const string GitHubReleasesUrl = "https://api.github.com/repos/RomanGod6/Ark-Ascended-Manager-Updater/releases";

        private async Task<List<string>> FetchReleaseNotesAsync()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "ArkAscendedManagerClientApplication");

                var response = await httpClient.GetAsync(GitHubReleasesUrl);
                if (response.IsSuccessStatusCode)
                {
                    var releasesJson = await response.Content.ReadAsStringAsync();
                    var releases = JArray.Parse(releasesJson);

                    return releases.Select(release => release["body"].ToString()).ToList();
                }
                else
                {
                    throw new InvalidOperationException("Could not fetch release notes from GitHub.");
                }
            }
        }

        public async void LoadReleaseNotes()
        {
            try
            {
                ReleaseNotes = "Loading release notes...";
                var releaseNotes = await FetchReleaseNotesAsync();
                ReleaseNotes = string.Join(Environment.NewLine + new string('-', 80) + Environment.NewLine, releaseNotes);
            }
            catch (Exception ex)
            {
                ReleaseNotes = "Failed to load release notes: " + ex.Message;
            }
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            CurrentTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
            AppVersion = $"Ark Ascended Manager - {GetAssemblyVersion()}";

            _isInitialized = true;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }
        private bool _groupConsoles;
        public bool GroupConsoles
        {
            get => _groupConsoles;
            set => SetProperty(ref _groupConsoles, value);
        }
        public class AAMGlobalSettings
        {
            public bool AutoUpdateServersOnReboot { get; set; }
            public bool AutoUpdateServersWhenNewUpdateAvailable { get; set; }
        }
        private AAMGlobalSettings _globalSettings;

        public AAMGlobalSettings GlobalSettings
        {
            get => _globalSettings;
            set
            {
                _globalSettings = value;
                OnPropertyChanged(nameof(GlobalSettings));
            }
        }

        private string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "AAMGlobalSettings.json");

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                GlobalSettings = JsonConvert.DeserializeObject<AAMGlobalSettings>(json) ?? new AAMGlobalSettings();
            }
            else
            {
                GlobalSettings = new AAMGlobalSettings();
            }
        }
        public bool AutoUpdateServersOnReboot
        {
            get => GlobalSettings.AutoUpdateServersOnReboot;
            set
            {
                if (GlobalSettings.AutoUpdateServersOnReboot != value)
                {
                    GlobalSettings.AutoUpdateServersOnReboot = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool AutoUpdateServersWhenNewUpdateAvailable
        {
            get => GlobalSettings.AutoUpdateServersWhenNewUpdateAvailable;
            set
            {
                if (GlobalSettings.AutoUpdateServersWhenNewUpdateAvailable != value)
                {
                    GlobalSettings.AutoUpdateServersWhenNewUpdateAvailable = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        private void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(GlobalSettings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }



        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == Wpf.Ui.Appearance.ThemeType.Light)
                        break;

                    Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Light);
                    CurrentTheme = Wpf.Ui.Appearance.ThemeType.Light;

                    break;

                default:
                    if (CurrentTheme == Wpf.Ui.Appearance.ThemeType.Dark)
                        break;

                    Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark);
                    CurrentTheme = Wpf.Ui.Appearance.ThemeType.Dark;

                    break;
            }
        }
    }
}
