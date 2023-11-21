using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using Ark_Ascended_Manager.Models;
using System.Windows.Input;

public class SyncConfigViewModel : INotifyPropertyChanged
{
    private ObservableCollection<ServerProfile> _serverProfiles;
    private ObservableCollection<ServerProfile> _filteredTargetServers;
    private ServerProfile _selectedSourceServer;
    public ICommand SyncCommand { get; private set; }


    public ObservableCollection<ServerProfile> ServerProfiles
    {
        get => _serverProfiles;
        set
        {
            _serverProfiles = value;
            OnPropertyChanged(nameof(ServerProfiles));
            FilterTargetServers();
        }
    }

    public ObservableCollection<ServerProfile> FilteredTargetServers
    {
        get => _filteredTargetServers;
        set
        {
            _filteredTargetServers = value;
            OnPropertyChanged(nameof(FilteredTargetServers));
        }
    }

    public ServerProfile SelectedSourceServer
    {
        get => _selectedSourceServer;
        set
        {
            if (_selectedSourceServer != value)
            {
                _selectedSourceServer = value;
                OnPropertyChanged(nameof(SelectedSourceServer));
                FilterTargetServers();
            }
        }
    }

    public SyncConfigViewModel()
    {
        LoadServerProfiles();
        SyncCommand = new RelayCommand(async () => await SyncSettingsAsync());
    }

    private void LoadServerProfiles()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string filePath = Path.Combine(appDataPath, "Ark Ascended Manager", "servers.json");

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            ServerProfiles = JsonConvert.DeserializeObject<ObservableCollection<ServerProfile>>(jsonContent) ?? new ObservableCollection<ServerProfile>();
        }
        else
        {
            ServerProfiles = new ObservableCollection<ServerProfile>();
        }
    }

    private void FilterTargetServers()
    {
        if (ServerProfiles == null) return;

        var filtered = ServerProfiles.Where(s => s != SelectedSourceServer).ToList();
        FilteredTargetServers = new ObservableCollection<ServerProfile>(filtered);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public async Task SyncSettingsAsync()
    {
        try
        {
            // Assume ServerProfile includes a property for the config directory path
            var sourcePath = Path.Combine(_selectedSourceServer.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer");
            var gameUserSettings = File.ReadAllText(Path.Combine(sourcePath, "GameUserSettings.ini"));
            var gameIni = File.ReadAllText(Path.Combine(sourcePath, "Game.ini"));

            var targetServers = _filteredTargetServers.Where(s => s != _selectedSourceServer).ToList();

            foreach (var server in targetServers)
            {
                var targetPath = Path.Combine(server.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer");
                File.WriteAllText(Path.Combine(targetPath, "GameUserSettings.ini"), gameUserSettings);
                File.WriteAllText(Path.Combine(targetPath, "Game.ini"), gameIni);
            }

            // Update the user about the sync process.
            MessageBox.Show($"Settings have been synchronized to {targetServers.Count} servers.", "Sync Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            // Handle exceptions, such as file I/O errors
            MessageBox.Show($"An error occurred: {ex.Message}", "Sync Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class ServerProfile
{
    public string ServerName { get; set; }
    public string ServerPath { get; set; }
    // Other properties...
}
