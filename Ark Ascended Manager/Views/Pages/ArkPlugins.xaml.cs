using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ArkPlugins : Page
    {
        private const string ApiUrl = "http://localhost:8080/cached_resources"; // Update this endpoint as needed
        private ObservableCollection<Resource> allPlugins;
        private ObservableCollection<Resource> currentPlugins;
        private int currentPage = 1;
        private int itemsPerPage = 10;

        public ArkPlugins()
        {
            InitializeComponent();
            LoadPlugins();
        }

        private async void LoadPlugins()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(ApiUrl);
                    System.IO.File.WriteAllText("debug.json", response); // Write the response to a file for debugging
                    allPlugins = JsonConvert.DeserializeObject<ObservableCollection<Resource>>(response);
                    DisplayPage(currentPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load plugins: {ex.Message}");
            }
        }

        private void DisplayPage(int pageNumber)
        {
            int start = (pageNumber - 1) * itemsPerPage;
            int count = Math.Min(itemsPerPage, allPlugins.Count - start);
            currentPlugins = new ObservableCollection<Resource>(allPlugins.SubList(start, count));
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
            var filteredPlugins = allPlugins.FindAll(p => p.Title.ToLower().Contains(query) || p.TagLine.ToLower().Contains(query) || (p.Tags != null && p.Tags.Exists(tag => tag.ToLower().Contains(query))));
            allPlugins = new ObservableCollection<Resource>(filteredPlugins);
            currentPage = 1;
            DisplayPage(currentPage);
        }
    }

    public static class Extensions
    {
        public static ObservableCollection<T> SubList<T>(this ObservableCollection<T> collection, int index, int count)
        {
            var result = new ObservableCollection<T>();
            for (int i = index; i < index + count && i < collection.Count; i++)
            {
                result.Add(collection[i]);
            }
            return result;
        }

        public static List<T> FindAll<T>(this ObservableCollection<T> collection, Predicate<T> match)
        {
            var result = new List<T>();
            foreach (var item in collection)
            {
                if (match(item))
                {
                    result.Add(item);
                }
            }
            return result;
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
        public string AltSupportURL { get; set; }
        public bool CanDownload { get; set; }
        public bool CanEdit { get; set; }
        public bool CanEditIcon { get; set; }
        public bool CanEditTags { get; set; }
        public bool CanHardDelete { get; set; }
        public bool CanSoftDelete { get; set; }
        public bool CanViewDescriptionAttachments { get; set; }
        public string Currency { get; set; }
        public Dictionary<string, object> CustomFields { get; set; }
        public int DownloadCount { get; set; }
        public string ExternalURL { get; set; }
        public string IconURL { get; set; }
        public bool IsWatching { get; set; }
        public int LastUpdate { get; set; }
        public string Prefix { get; set; }
        public int PrefixID { get; set; }
        public string Price { get; set; }
        public double RatingAvg { get; set; }
        public int RatingCount { get; set; }
        public double RatingWeighted { get; set; }
        public int ResourceCategoryID { get; set; }
        public int ResourceDate { get; set; }
        public int ResourceID { get; set; }
        public string ResourceState { get; set; }
        public string ResourceType { get; set; }
        public string TagLine { get; set; }
        public List<string> Tags { get; set; }
        public string Title { get; set; }
        public User User { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Version { get; set; }
        public int ViewCount { get; set; }
        public string ViewURL { get; set; }
    }
}
