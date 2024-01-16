using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class CurseForgeModPage : Page
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiKey = "$2a$10$CWRB9.VQ7liAu.RrYBVw3.UdssWCj8dw.jv2SXMAZx1/UYLf1VmYW"; // Replace with your actual API key
        private const int pageSize = 50; // Number of items per page
        private int currentPageIndex = 0; // Zero-based index for API
        private int totalAvailableResults = 0; // Total available results from the API
        private readonly INavigationService _navigationService;
        public ObservableCollection<Mod> Mods { get; private set; } = new ObservableCollection<Mod>();

        public CurseForgeModPage(INavigationService navigationService)
        {
            InitializeComponent();
            DataContext = this;
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            FetchModsAsync(); // Load mods when the page is initialized
        }

        private async Task FetchModsAsync(string searchFilter = "")
        {
            // Calculate the current page index for the API call
            int page = (currentPageIndex * pageSize) < totalAvailableResults ? currentPageIndex : 0;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);

            var url = $"https://api.curseforge.com/v1/mods/search?gameId=83374&pageSize={pageSize}&index={page}";
            if (!string.IsNullOrEmpty(searchFilter))
            {
                url += $"&searchFilter={searchFilter}";
            }

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var modsResponse = JsonConvert.DeserializeObject<ModsResponse>(content);
                if (modsResponse?.Data != null)
                {
                    Mods.Clear(); // Clear the existing mods
                    foreach (var mod in modsResponse.Data)
                    {
                        Mods.Add(mod);
                    }

                    // Update pagination details
                    currentPageIndex = modsResponse.Pagination.Index;
                    totalAvailableResults = modsResponse.Pagination.TotalCount;
                }
            }
            else
            {
                MessageBox.Show($"Error fetching mods: {response.ReasonPhrase}");
            }
        }
        public void OnNavigatedTo(int modId)
        {
          
            SaveModIdToJson(modId);
        }
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            currentPageIndex = 0; // Reset the current page index to start from the first page
            await FetchModsAsync(SearchTextBox.Text.Trim());
        }


        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if ((currentPageIndex + 1) * pageSize < totalAvailableResults)
            {
                currentPageIndex++;
                await FetchModsAsync(SearchTextBox.Text.Trim());
            }
        }
        private void ModListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Mod selectedMod = e.AddedItems[0] as Mod;
                if (selectedMod != null)
                {
                    Debug.WriteLine(ModSelectionService.CurrentModId);
                    ModSelectionService.CurrentModId = 0; // Reset to trigger change
                    Debug.WriteLine(ModSelectionService.CurrentModId);
                    ModSelectionService.CurrentModId = selectedMod.Id; // Set new mod ID
                    Debug.WriteLine(ModSelectionService.CurrentModId);
                    NavigateToModDetailsPage(); // Navigate to details page
                }
            }
        }

        public static class ModSelectionService
        {
            public static int CurrentModId { get; set; }
        }

        private void SaveModIdToJson(int modId)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directoryPath = Path.Combine(appDataFolder, "Ark Ascended Manager");
            Directory.CreateDirectory(directoryPath); // Ensure the directory exists

            string filePath = Path.Combine(directoryPath, "currentmod.json");
            string json = JsonConvert.SerializeObject(new { ModId = modId });
            File.WriteAllText(filePath, json);
        }

        private void NavigateToModDetailsPage()
        {
            
            _navigationService.Navigate(typeof(CurseForgeCurrentModPage));
            
        }


        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                await FetchModsAsync(SearchTextBox.Text.Trim());
            }
        }

    }
   
        public class UrlToBitmapImageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var url = value as string;
                if (string.IsNullOrEmpty(url))
                {
                    // Use the fallback image
                    var fallbackImage = new BitmapImage(new Uri("pack://application:,,,/Assets/AAM_Icon.png", UriKind.Absolute));
                    fallbackImage.Freeze(); // Freeze for thread safety
                    return fallbackImage;
                }
                else
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(url, UriKind.RelativeOrAbsolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    if (image.CanFreeze)
                    {
                        // Only freeze if the image can be frozen
                        image.Freeze(); // Freeze for thread safety
                    }
                    return image;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }



        // Your existing Mod, Links, ModsResponse

        public class Mod
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public Links Links { get; set; }
        public string Summary { get; set; }


        [JsonProperty("logo")]
        public Logo Logo { get; set; }

        [JsonIgnore]
        public string ThumbnailUrl => Logo?.ThumbnailUrl;
        // ... include other properties as needed
    }

    public class Links
    {
        public string WebsiteUrl { get; set; }
        public string WikiUrl { get; set; }
        public string IssuesUrl { get; set; }
        public string SourceUrl { get; set; }
    }

    public class Logo
    {
        [JsonProperty("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }
        // ...other properties...
    }


    public class ModsResponse
    {
        public List<Mod> Data { get; set; }
        public Pagination Pagination { get; set; }
    }

    public class Pagination
    {
        public int Index { get; set; }
        public int PageSize { get; set; }
        public int ResultCount { get; set; }
        public int TotalCount { get; set; }
    }

    // And so on for other nested objects as per your JSON structure...


}
