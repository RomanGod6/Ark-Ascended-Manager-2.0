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

    public BasicResource SelectedPlugin { get; private set; }

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
        string currentPluginPath = Path.Combine(arkAscendedManagerPath, "CurrentPlugin.json");

        if (File.Exists(currentPluginPath))
        {
            string json = File.ReadAllText(currentPluginPath);
            return JsonConvert.DeserializeObject<BasicResource>(json);
        }

        return null; // or handle the case where the file doesn't exist
    }



    public void OnNavigatedTo(object parameter)
    {
        if (parameter is BasicResource newPlugin)
        {
            var localData = LoadCurrentPluginData();
            if (localData != null)
            {
                SelectedPlugin = localData;
                OnPropertyChanged(nameof(SelectedPlugin));
            }

            // Then fetch the latest details
            _ = FetchLatestPluginDetailsAsync(newPlugin.ResourceId);
        }
        else
        {
            // Handle unexpected parameter
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
