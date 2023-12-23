using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Ark_Ascended_Manager.Models;
using static PluginsPageViewModel;

public class PluginInstallPageViewModel : INotifyPropertyChanged
{
    private HttpClient _httpClient = new HttpClient();
    private string _apiKey = "bpqpPHZ71cHzEP3AVSuTmLkqXvKoSBj-"; // Replace with your actual API key

    private BasicResource _selectedPlugin;

    public BasicResource SelectedPlugin
    {
        get => _selectedPlugin;
        set
        {
            if (_selectedPlugin != value)
            {
                _selectedPlugin = value;
                OnPropertyChanged(nameof(SelectedPlugin));
            }
        }
    }

    public PluginInstallPageViewModel()
    {
        // You might want to move the API key setup and HttpClient initialization to a more secure place
        _httpClient.DefaultRequestHeaders.Add("XF-Api-Key", _apiKey);
        LoadCurrentPluginData();
    }
    public async Task SaveCurrentPluginDataAsync(BasicResource pluginData)
    {
        string json = JsonConvert.SerializeObject(pluginData);
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string arkAscendedManagerPath = Path.Combine(appDataPath, "Ark Ascended Manager");
        string currentPluginPath = Path.Combine(arkAscendedManagerPath, "CurrentPlugin.json");

        Directory.CreateDirectory(arkAscendedManagerPath); // Ensure directory exists
        File.WriteAllText(currentPluginPath, json);
    }

    public async Task FetchLatestPluginDetailsAsync(int resourceId)
    {
        string uri = $"https://gameservershub.com/forums/api/framework/resources/{resourceId}/updates/latest";

        try
        {
            var response = await _httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Fetched plugin details successfully: " + content);

                SelectedPlugin = JsonConvert.DeserializeObject<BasicResource>(content);
                OnPropertyChanged(nameof(SelectedPlugin));

                await SaveCurrentPluginDataAsync(SelectedPlugin); // Save the fetched data
            }
            else
            {
                Debug.WriteLine($"Error fetching plugin details: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception occurred while fetching plugin details: {ex.Message}");
        }
    }


    public BasicResource LoadCurrentPluginData()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string arkAscendedManagerPath = Path.Combine(appDataPath, "Ark Ascended Manager");
        string currentPluginPath = Path.Combine(arkAscendedManagerPath, "SelectedPlugin.json");

        if (File.Exists(currentPluginPath))
        {
            string json = File.ReadAllText(currentPluginPath);
            Debug.WriteLine("Loaded current plugin data: " + json);
            return JsonConvert.DeserializeObject<BasicResource>(json);
        }
        else
        {
            Debug.WriteLine("No current plugin data found.");
            return null; // or handle the case where the file doesn't exist
        }
    }





    public void OnNavigatedTo(object parameter)
    {

        if (parameter is BasicResource newPlugin)
        {
            SelectedPlugin = newPlugin;
            OnPropertyChanged(nameof(SelectedPlugin));
            Debug.WriteLine("SelectedPlugin assigned: " + (SelectedPlugin != null));
        }
        else
        {
            Debug.WriteLine("Invalid parameter type in OnNavigatedTo.");
        }
    }




    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
}
