using Wpf.Ui.Controls;
using System.Collections.Generic; // If using collections
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public partial class ImportServersPageViewModel : ObservableObject, INavigationAware, INotifyPropertyChanged
    {
        // Define properties and methods relevant to managing servers

        public void OnNavigatedTo()
        {
            // Initialization logic specific to the servers page
        }

        public void OnNavigatedFrom() { }
        private string _serverPath;

        public string ServerPath
        {
            get { return _serverPath; }
            set
            {
                if (_serverPath != value)
                {
                    _serverPath = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _serverName;
        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        private int _listenPort;
        public int ListenPort
        {
            get => _listenPort;
            set => SetProperty(ref _listenPort, value);
        }
        private int _rconPort;
        public int RCONPort
        {
            get => _rconPort;
            set => SetProperty(ref _rconPort, value);
        }

        private string _mods; // Assuming Mods is a single string of comma-separated values
        public string Mods
        {
            get => _mods;
            set => SetProperty(ref _mods, value);
        }

        private string _adminPassword;
        public string AdminPassword
        {
            get => _adminPassword;
            set => SetProperty(ref _adminPassword, value);
        }

        private string _serverPassword;
        public string ServerPassword
        {
            get => _serverPassword;
            set => SetProperty(ref _serverPassword, value);
        }

        private bool _useBattlEye;
        public bool UseBattlEye
        {
            get => _useBattlEye;
            set => SetProperty(ref _useBattlEye, value);
        }

        private bool _forceRespawnDinos;
        public bool ForceRespawnDinos
        {
            get => _forceRespawnDinos;
            set => SetProperty(ref _forceRespawnDinos, value);
        }

        private bool _preventSpawnAnimation;
        public bool PreventSpawnAnimation
        {
            get => _preventSpawnAnimation;
            set => SetProperty(ref _preventSpawnAnimation, value);
        }
        private int _maxPlayerCount;
        public int MaxPlayerCount
        {
            get => _maxPlayerCount;
            set => SetProperty(ref _maxPlayerCount, value);
        }


        // ... Additional properties if necessary ...

        // This method is used to raise the property changed event
        protected virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private List<string> _optionsList = new List<string> { "TheIsland_WP" }; // Example options
        public List<string> OptionsList
        {
            get { return _optionsList; }
            set
            {
                if (_optionsList != value)
                {
                    _optionsList = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _selectedOption;
        public string SelectedOption
        {
            get { return _selectedOption; }
            set
            {
                if (_selectedOption != value)
                {
                    _selectedOption = value;
                    OnPropertyChanged();
                }
            }
        }


        // Other properties and methods for your ViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _profileName;
        public string ProfileName
        {
            get { return _profileName; }
            set
            {
                if (_profileName != value)
                {
                    _profileName = value;
                    OnPropertyChanged();
                }
            }
        }

        private Dictionary<string, string> _mapToAppId = new Dictionary<string, string>()
        {
            { "TheIsland_WP", "2430930" }
            // Add more mappings here
        };

        public Dictionary<string, string> MapToAppId
        {
            get { return _mapToAppId; }
            // Read-only if you do not plan to change it dynamically
        }
     

    }
}
