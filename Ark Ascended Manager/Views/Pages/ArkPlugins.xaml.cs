using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Ark_Ascended_Manager.Helpers;
using System.Windows.Navigation;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ArkPlugins : Page
    {
        private const string ApiUrl = "http://localhost:8080/cached_resources"; // Update this endpoint as needed
        private ObservableCollection<Resource> allPlugins;
        private ObservableCollection<Resource> currentPlugins;
        private int currentPage = 1;
        private int itemsPerPage = 10;
        private DatabaseHelper databaseHelper;

        public ArkPlugins()
        {
            InitializeComponent();
            databaseHelper = new DatabaseHelper();
            LoadPluginsFromDatabase();
            LoadPlugins();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private async void LoadPlugins()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(ApiUrl);
                    string currentDirectory = Directory.GetCurrentDirectory();
                    allPlugins = JsonConvert.DeserializeObject<ObservableCollection<Resource>>(response);
                    databaseHelper.SavePlugins(allPlugins);
                    DisplayPage(currentPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load plugins: {ex.Message}");
            }
        }

        private void LoadPluginsFromDatabase()
        {
            allPlugins = databaseHelper.LoadPlugins();
            Debug.WriteLine($"Loaded {allPlugins.Count} plugins from database.");
            DisplayPage(currentPage);
        }
        private void PluginsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PluginsListView.SelectedItem is Resource selectedResource)
            {
                var resourcePage = new ArkPluginResource(selectedResource);
                NavigationService.Navigate(resourcePage);
            }
        }
        private void DisplayPage(int pageNumber)
        {
            int start = (pageNumber - 1) * itemsPerPage;
            int count = Math.Min(itemsPerPage, allPlugins.Count - start);
            currentPlugins = new ObservableCollection<Resource>(allPlugins.Skip(start).Take(count));
            PluginsListView.ItemsSource = currentPlugins;
            PageInfo.Text = $"Page {currentPage} of {Math.Ceiling((double)allPlugins.Count / itemsPerPage)}";
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                DisplayPage(currentPage);
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < Math.Ceiling((double)allPlugins.Count / itemsPerPage))
            {
                currentPage++;
                DisplayPage(currentPage);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text.ToLower();
            var filteredPlugins = allPlugins.Where(p => p.Title.ToLower().Contains(query) ||
                                                         p.TagLine.ToLower().Contains(query) ||
                                                         (p.Tags != null && p.Tags.Any(tag => tag.ToLower().Contains(query)))).ToList();
            allPlugins = new ObservableCollection<Resource>(filteredPlugins);
            currentPage = 1;
            DisplayPage(currentPage);
        }
    }



public class User
    {
        public Dictionary<string, string> AvatarURLs { get; set; }
        public bool CanBan { get; set; }
        public bool CanConverse { get; set; }
        public bool CanEdit { get; set; }
        public bool CanFollow { get; set; }
        public bool CanIgnore { get; set; }
        public bool CanPostProfile { get; set; }
        public bool CanViewProfile { get; set; }
        public bool CanViewProfilePosts { get; set; }
        public bool CanWarn { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public bool IsFollowed { get; set; }
        public bool IsIgnored { get; set; }
        public bool IsStaff { get; set; }
        public int LastActivity { get; set; }
        public string Location { get; set; }
        public int MessageCount { get; set; }
        public Dictionary<string, string> ProfileBannerURLs { get; set; }
        public int QuestionSolutionCount { get; set; }
        public int ReactionScore { get; set; }
        public int RegisterDate { get; set; }
        public string Signature { get; set; }
        public int TrophyPoints { get; set; }
        public int UserID { get; set; }
        public string UserTitle { get; set; }
        public string Username { get; set; }
        public string ViewURL { get; set; }
        public int VoteScore { get; set; }
        public string Website { get; set; }
    }

    public class Resource
    {
        [JsonProperty("alt_support_url")]
        public string AltSupportURL { get; set; }

        [JsonProperty("icon_url")]
        public string IconURL { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("resource_id")]
        public string ResourceID { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("rating_avg")]
        public double RatingAvg { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("tag_line")]
        public string TagLine { get; set; }

        [JsonProperty("external_url")]
        public string ExternalURL { get; set; }

        [JsonProperty("view_url")]
        public string ViewURL { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("user_id")]
        public int UserID { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("view_count")]
        public int ViewCount { get; set; }

        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }

        [JsonProperty("rating_count")]
        public int RatingCount { get; set; }

        [JsonProperty("rating_weighted")]
        public double RatingWeighted { get; set; }

        [JsonProperty("resource_category_id")]
        public int ResourceCategoryID { get; set; }

        [JsonProperty("resource_date")]
        public long ResourceDate { get; set; }

        [JsonProperty("resource_state")]
        public string ResourceState { get; set; }

        [JsonProperty("resource_type")]
        public string ResourceType { get; set; }
    }

}
