

using Ark_Ascended_Manager.Views.Pages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Windows.Input;
using Wpf.Ui.Controls;
using System.IO;
using Ark_Ascended_Manager.Views.Windows;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Diagnostics;

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
            public string UpdateCountdownTimer { get; set; }

            public int UpdateCheckInterval { get; set; } = 10;
            public string CurrentTheme { get; set; } = "Light";
            public string Language { get; set; } = "en";
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

        public string Language
        {
            get => GlobalSettings.Language;
            set
            {
                if (GlobalSettings.Language != value)
                {
                    GlobalSettings.Language = value;
                    OnPropertyChanged();
                    ApplyLanguage(value);
                    SaveSettings();
                }
            }
        }

        private string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "AAMGlobalSettings.json");

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                Debug.WriteLine(json);
                GlobalSettings = JsonConvert.DeserializeObject<AAMGlobalSettings>(json) ?? new AAMGlobalSettings();
                ApplyTheme(GlobalSettings.CurrentTheme); // Apply theme based on loaded setting
                ApplyLanguage(GlobalSettings.Language);
            }
            else
            {
                GlobalSettings = new AAMGlobalSettings();
            }
        }
        public string UpdateCountdownTimer
        {
            get => GlobalSettings.UpdateCountdownTimer;
            set
            {
                if (GlobalSettings.UpdateCountdownTimer != value)
                {
                    GlobalSettings.UpdateCountdownTimer = value;
                    OnPropertyChanged();
                    SaveSettings(); 
                }
            }
        }

        public int UpdateCheckInterval
        {
            get => GlobalSettings.UpdateCheckInterval;
            set
            {
                if (GlobalSettings.UpdateCheckInterval != value)
                {
                    GlobalSettings.UpdateCheckInterval = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public void ApplyLanguage(string languageCode)
        {
            try
            {
                CultureInfo culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // Optionally refresh the main window to apply the new language
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.Language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
            }
            catch (CultureNotFoundException ex)
            {
                // Handle the exception if the culture is not found
                Console.WriteLine($"Culture {languageCode} is not supported: {ex.Message}");
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
        private void ApplyTheme(string theme)
        {
            Wpf.Ui.Appearance.ThemeType themeType = theme == "Light" ? Wpf.Ui.Appearance.ThemeType.Light : Wpf.Ui.Appearance.ThemeType.Dark;
            Wpf.Ui.Appearance.Theme.Apply(themeType);
            CurrentTheme = themeType;
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(GlobalSettings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedComboBoxItem = e.AddedItems[0] as ComboBoxItem;
                if (selectedComboBoxItem != null)
                {
                    string selectedLanguage = selectedComboBoxItem.Tag.ToString();
                    GlobalSettings.Language = selectedLanguage;
                    ApplyLanguage(selectedLanguage);
                    SaveSettings();
                }
            }
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
            GlobalSettings.CurrentTheme = CurrentTheme.ToString();
            SaveSettings();
        }

    }
}
