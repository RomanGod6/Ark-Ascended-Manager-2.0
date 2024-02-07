using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class PluginManagementPage : Page 
    {
        private IWebDriver driver;
        private string lastDownloadedUrl;
        private readonly INavigationService _navigationService;
        public PluginManagementPage(INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Specify a user data folder within the user's AppData directory
            string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "WebView2");

            // Ensure the directory exists
            Directory.CreateDirectory(userDataFolder);

            // Create a new CoreWebView2Environment with the specified user data folder
            CoreWebView2Environment webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            // Initialize the WebView2 control with the created environment
            await webView.EnsureCoreWebView2Async(webView2Environment);

            // Navigate to the website where you want to automate actions
            webView.CoreWebView2.Navigate("https://gameservershub.com/");
        }


        // Event handler for the "Install Plugin" button click
        // Event handler for the "Install Plugin" button click
        private async void InstallPluginButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the current URL from the WebView2 control
            string currentUrl = webView.CoreWebView2.Source.ToString();

            // Use the current URL as the pluginPageUrl
            string pluginPageUrl = currentUrl;

            // Construct the plugin download URL
            string downloadUrl = pluginPageUrl + "download";

            // Check if the download URL is already in the downloaded_urls.json file
            bool isAlreadyDownloaded = IsDownloaded(downloadUrl);

            if (isAlreadyDownloaded)
            {
                MessageBox.Show("Plugin is already added to the master list.", "Plugin Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Navigate to the download URL in the WebView2 control
                webView.CoreWebView2.Navigate(downloadUrl);

                // Wait for the download to complete or timeout after 30 seconds (adjust as needed)
                bool downloadCompleted = await WaitForDownloadCompletion(30000);

                if (downloadCompleted)
                {
                    // Check for the latest downloaded .zip file in the Downloads folder
                    string downloadsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\";
                    string[] zipFiles = Directory.GetFiles(downloadsFolderPath, "*.zip");

                    if (zipFiles.Length > 0)
                    {
                        // Sort the files by last modified date to get the latest one
                        Array.Sort(zipFiles, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

                        // Get the path of the latest .zip file
                        string latestZipFile = zipFiles[0];

                        // Specify the destination directory within the Roaming folder
                        string destinationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "plugins");

                        // Ensure the destination directory exists, create it if it doesn't
                        Directory.CreateDirectory(destinationDirectory);

                        // Combine the destination directory with the file name
                        string destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(latestZipFile));

                        // Move the latest .zip file to the destination
                        File.Move(latestZipFile, destinationPath);

                        // Store the full download URL in JSON
                        lastDownloadedUrl = downloadUrl;
                        SaveLastDownloadedUrl(destinationDirectory, "downloaded_urls.json");

                        // Handle the moved file and stored URL as needed
                        MessageBox.Show("Downloaded file moved to: " + destinationPath + "\nLast Downloaded URL: " + lastDownloadedUrl, "File Moved", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // No .zip files found in Downloads folder, handle as needed
                        MessageBox.Show("No .zip files found in Downloads folder.", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Download timed out, handle as needed
                    MessageBox.Show("Download timed out or failed.", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void GoToAutoInstallPageButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the PluginManagementAutoInstallPage
            _navigationService.Navigate(typeof(PluginManagementAutoInstallPage));
        }

        private bool IsDownloaded(string downloadUrl)
        {
            // Define the directory path where downloaded_urls.json is stored
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "plugins");

            // Combine the directory path with the JSON file name
            string filePath = Path.Combine(directoryPath, "downloaded_urls.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                List<string> downloadedUrls = JsonConvert.DeserializeObject<List<string>>(json);

                // Check if any existing URL matches the given downloadUrl (ignoring query parameters and trailing slashes)
                bool isDuplicate = downloadedUrls.Any(existingUrl =>
                    Uri.TryCreate(existingUrl, UriKind.Absolute, out Uri existingUri) &&
                    Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri newUri) &&
                    Uri.Compare(existingUri, newUri, UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0);

                return isDuplicate;
            }

            return false;
        }
       

        public void DownloadFile(string fileUrl, string destinationPath)
        {
            driver = new ChromeDriver();
            try
            {
                // Navigate to the URL
                driver.Navigate().GoToUrl(fileUrl);

                // You can interact with the webpage here if necessary (e.g., click buttons)

                // Wait for the file to download (monitor the download directory)
                WaitForDownloadToComplete(destinationPath);

                // Notify the user about successful download
                Console.WriteLine($"API downloaded to {destinationPath}");
            }
            catch (Exception ex)
            {
                // Handle any errors during the process
                Console.WriteLine($"Error during download: {ex.Message}");
            }
            finally
            {
                // Quit the browser session
                driver.Quit();
            }
        }

        private void WaitForDownloadToComplete(string destinationPath)
        {
            string defaultDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            string downloadedFilePath = Path.Combine(defaultDownloadDirectory, "OfficialAPI.zip");

            // Wait for a maximum of 5 minutes for the file to appear in the download directory
            DateTime startTime = DateTime.Now;
            while (!File.Exists(downloadedFilePath))
            {
                // Check if the maximum waiting time has been reached (adjust as needed)
                if ((DateTime.Now - startTime).TotalMinutes >= 5)
                {
                    throw new Exception("Download timeout. File did not appear in the download directory.");
                }

                // Sleep for a short duration before checking again
                System.Threading.Thread.Sleep(1000);
            }

            // Move the downloaded file to the desired destination
            File.Move(downloadedFilePath, destinationPath);
        }





        private void SaveLastDownloadedUrl(string directoryPath, string jsonFileName)
        {
            // Deserialize existing JSON file if it exists, or create a new list
            List<string> downloadedUrls = new List<string>();

            string filePath = Path.Combine(directoryPath, jsonFileName);

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                downloadedUrls = JsonConvert.DeserializeObject<List<string>>(json);
            }

            // Append the new URL to the list
            downloadedUrls.Add(lastDownloadedUrl);

            // Serialize the updated list and save it as a JSON file
            string updatedJson = JsonConvert.SerializeObject(downloadedUrls);
            File.WriteAllText(filePath, updatedJson);
        }



        private async Task<bool> WaitForDownloadCompletion(int timeoutMilliseconds)
        {
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMilliseconds)
            {
                await Task.Delay(1000); // Check every second

                // Check if the latest .zip file is found in the Downloads folder
                string downloadsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\";
                string[] zipFiles = Directory.GetFiles(downloadsFolderPath, "*.zip");

                if (zipFiles.Length > 0)
                {
                    // Download is completed
                    return true;
                }
            }

            // Download timed out
            return false;
        }


       

        // Ensure that you handle WebView2 disposal when the page is no longer needed
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (webView != null)
            {
                webView.Dispose();
            }
        }
    }
}
