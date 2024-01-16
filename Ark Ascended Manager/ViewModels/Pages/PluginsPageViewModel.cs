using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Ark_Ascended_Manager.Views.Pages;

public class PluginsPageViewModel : INotifyPropertyChanged
{
    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiEndpoint = "https://gameservershub.com/forums/api/framework/resources";

    public ObservableCollection<BasicResource> Plugins { get; private set; } = new ObservableCollection<BasicResource>();
    public ICommand MoreInfoCommand { get; private set; }
    private readonly INavigationService _navigationService;

    public event PropertyChangedEventHandler PropertyChanged;

    public PluginsPageViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        MoreInfoCommand = new RelayCommand<BasicResource>(OpenPluginDetails);
        string apiKey = "57iSQzK_r0wY77gsPTB3S29F0069s6YF";
        _httpClient.DefaultRequestHeaders.Add("XF-Api-Key", apiKey);
        FetchAndSavePluginsAsync();
    }
    public BasicResource SelectedPlugin { get; set; }
    private void OpenPluginDetails(BasicResource selectedPlugin)
    {
        // Create the SelectedPlugin object using the properties from the selectedPlugin
        SelectedPlugin = new BasicResource
        {
            ResourceId = selectedPlugin.ResourceId,
            AuthorId = selectedPlugin.AuthorId,
            Title = selectedPlugin.Title,
            Message = selectedPlugin.Message,
            ReleaseDate = selectedPlugin.ReleaseDate,
            LastUpdateDate = selectedPlugin.LastUpdateDate,
            CategoryTitle = selectedPlugin.CategoryTitle,
            CurrentVersionId = selectedPlugin.CurrentVersionId,
            Price = selectedPlugin.Price,
            Currency = selectedPlugin.Currency,
            DownloadCount = selectedPlugin.DownloadCount,
            ReviewCount = selectedPlugin.ReviewCount,
            ReviewAverage = selectedPlugin.ReviewAverage
        };

        // Assuming you have a 'SelectedPlugin' object that you want to save
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "Ark Ascended Manager");
        Directory.CreateDirectory(appFolder);
        string pluginDetailsPath = Path.Combine(appFolder, "SelectedPlugin.json");
        string json = JsonConvert.SerializeObject(SelectedPlugin);
        File.WriteAllText(pluginDetailsPath, json);

        _navigationService.Navigate(typeof(PluginInstallPage));
    }






    public async Task FetchAndSavePluginsAsync()
    {
        var response = await _httpClient.GetAsync(ApiEndpoint);
        if (response.IsSuccessStatusCode)
            
        {
            Debug.WriteLine("Response was successful.");
            var json = await response.Content.ReadAsStringAsync();
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "Ark Ascended Manager");
            Directory.CreateDirectory(appFolder);
            string uncleanPath = Path.Combine(appFolder, "app_data_unclean.json");
            string cleanPath = Path.Combine(appFolder, "app_data_clean.json");

            try
            {
                File.WriteAllText(uncleanPath, json);
                Ark_Ascended_Manager.Services.Logger.Log($"Successfully saved unclean data to {uncleanPath}.");
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Failed to save unclean data: {ex.Message}");
            }

            try
            {
                var jsonObject = JObject.Parse(json);
                var allPluginsArray = (JArray)jsonObject["Resource"];
                var groupedPlugins = allPluginsArray
                    .GroupBy(p => (int)p["resource_id"])
                    .Select(g => g.OrderByDescending(p => (long)p["last_update_date"]).First())
                    .Where(p => p["category_title"]?.ToString().StartsWith("ASA:") == true)
                    .ToList();

                var asaPlugins = JsonConvert.SerializeObject(groupedPlugins);

                File.WriteAllText(cleanPath, asaPlugins);
                Ark_Ascended_Manager.Services.Logger.Log($"Successfully saved clean data to {cleanPath}.");

                // Update the ObservableCollection on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Plugins.Clear();
                    foreach (var plugin in groupedPlugins)
                    {
                        Plugins.Add(new BasicResource
                        {
                            // Map the properties from the JSON to the BasicResource class properties here
                            ResourceId = plugin.Value<int>("resource_id"),
                            AuthorId = plugin.Value<int>("author_id"),
                            Title = plugin.Value<string>("title"),
                            Message = plugin.Value<string>("message"),
                            ReleaseDate = plugin.Value<long>("release_date"),
                            LastUpdateDate = plugin.Value<long>("last_update_date"),
                            CategoryTitle = plugin.Value<string>("category_title"),
                            CurrentVersionId = plugin.Value<int>("current_version_id"),
                            Price = plugin.Value<string>("price"),
                            Currency = plugin.Value<string>("currency"),
                            DownloadCount = plugin.Value<int>("download_count"),
                            ReviewCount = plugin.Value<int>("review_count"),
                            ReviewAverage = plugin.Value<double>("review_average"),
                        });
                    }

                    OnPropertyChanged(nameof(Plugins));
                });
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception occurred: {ex.ToString()}");
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Ark_Ascended_Manager.Services.Logger.Log($"Error fetching data: {response.StatusCode} - {response.ReasonPhrase}\nResponse content: {errorContent}");
        }

    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class BasicResource
    {
        public int ResourceId { get; set; }
        public int AuthorId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public long ReleaseDate { get; set; }
        public long LastUpdateDate { get; set; }
        public string CategoryTitle { get; set; }
        public int CurrentVersionId { get; set; }
        public string Price { get; set; }
        public string Currency { get; set; }
        public int DownloadCount { get; set; }
        public int ReviewCount { get; set; }
        public double ReviewAverage { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }

        // You may want to convert the Unix timestamp to a human-readable date
        public DateTime ReleaseDateTime => DateTimeOffset.FromUnixTimeSeconds(ReleaseDate).DateTime;
        public DateTime LastUpdateDateTime => DateTimeOffset.FromUnixTimeSeconds(LastUpdateDate).DateTime;

        // Additional properties for binding in XAML, if needed
        // These properties should correspond to what you have in your XAML bindings
        // For example, if you have an ImagePath in your XAML bindings, you should have a property for it here.
        // public string ImagePath { get; set; }
        // public string Description { get; set; }
        // ...

        // Implement any additional logic or methods required for your UI
        // For example, if you need to format the date or calculate something based on the properties
    }
}
