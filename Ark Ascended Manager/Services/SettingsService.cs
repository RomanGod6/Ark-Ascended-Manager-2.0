using Newtonsoft.Json;
using System;
using System.IO;
using Wpf.Ui.Appearance;

namespace Ark_Ascended_Manager.Services
{
    internal class SettingsService : ISettingsService
    {
        private string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "AAMGlobalSettings.json");
        private AAMGlobalSettings _settings;

        public ThemeType CurrentTheme
        {
            get => _settings?.CurrentTheme ?? ThemeType.Light; // Default to Light if null
            set
            {
                if (_settings == null) _settings = new AAMGlobalSettings();
                _settings.CurrentTheme = value;
                SaveSettings();
            }
        }

        public SettingsService()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _settings = JsonConvert.DeserializeObject<AAMGlobalSettings>(json);
            }
            else
            {
                _settings = new AAMGlobalSettings();
            }
        }

        private void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }

        // Definition for AAMGlobalSettings
        private class AAMGlobalSettings
        {
            public ThemeType CurrentTheme { get; set; } = ThemeType.Light; // Default to Light
        }
    }
}
