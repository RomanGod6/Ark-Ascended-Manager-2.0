﻿using Ark_Ascended_Manager.Helpers;
using Ark_Ascended_Manager.Views.Pages;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using HtmlAgilityPack;
using System.Windows.Documents; 
using System.Text; 
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using static Ark_Ascended_Manager.Views.Pages.CurseForgeModPage;



namespace Ark_Ascended_Manager.Views.Pages
{

    public partial class CurseForgeCurrentModPage : Page
    {
        private int _modId;
        private Mod _currentMod;
        private readonly INavigationService _navigationService;

        public CurseForgeCurrentModPage(INavigationService navigationService)
        {
           
            InitializeComponent();
            _modId = ModSelectionService.CurrentModId;
            Debug.WriteLine(ModSelectionService.CurrentModId);
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            LoadModDetails();
        }

        private int ReadModIdFromJson()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "currentmod.json");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The current mod configuration file was not found.");
            }

            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            return (int)data.ModId;
        }

        public static FlowDocument ConvertHtmlToFlowDocument(string jsonDescription)
        {
            // Extract HTML content from JSON
            var jsonObject = JObject.Parse(jsonDescription);
            string htmlContent = jsonObject["data"].ToString();

            // Now convert the HTML to XAML
            var converter = new HtmlToXamlConverter();
            string xamlContent = converter.ConvertHtmlToXaml(htmlContent);

            try
            {
                // Check if xamlContent already contains a FlowDocument tag
                if (!xamlContent.TrimStart().StartsWith("<FlowDocument"))
                {
                    xamlContent = $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{xamlContent}</FlowDocument>";
                }

                // Create a ParserContext with necessary namespace mappings
                var context = new ParserContext();
                context.XamlTypeMapper = new XamlTypeMapper(new string[0]);
                context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

                // Load the XAML as a FlowDocument using the context
                Debug.WriteLine("Converted XAML: " + xamlContent); // Log the XAML content

                var flowDoc = XamlReader.Parse(xamlContent, context) as FlowDocument;
                return flowDoc;
            }
            catch (XamlParseException ex)
            {
                Debug.WriteLine("Error parsing XAML: " + ex.Message);
                Debug.WriteLine("Inner Exception: " + ex.InnerException?.Message); // Log more details
                return new FlowDocument(); // Return an empty FlowDocument in case of error
            }
        }
       
        public void OnNavigatedTo(object parameter)
        {
            ReadModIdFromJson();
        }








        private void AddModButton_Click(object sender, RoutedEventArgs e)
        {
    
         _navigationService.Navigate(typeof(AddModToServerPage));



            
   
        }










        private async void LoadModDetails()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", "$2a$10$CWRB9.VQ7liAu.RrYBVw3.UdssWCj8dw.jv2SXMAZx1/UYLf1VmYW");

            // Fetch mod details
            var modResponse = await httpClient.GetAsync($"https://api.curseforge.com/v1/mods/{_modId}");

            if (modResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await modResponse.Content.ReadAsStringAsync();
                var modDetails = JsonConvert.DeserializeObject<ModResponse>(jsonResponse).Data;

                // Fetch mod description
                var descriptionResponse = await httpClient.GetAsync($"https://api.curseforge.com/v1/mods/{_modId}/description");

                if (descriptionResponse.IsSuccessStatusCode)
                {
                    var description = await descriptionResponse.Content.ReadAsStringAsync();
                    modDetails.Description = description;

                    // Set DataContext with mod details
                    this.DataContext = modDetails;

                    // Create an instance of HtmlToXamlConverter
                    var converter = new HtmlToXamlConverter();

                    // Convert HTML to FlowDocument
                    FlowDocument flowDoc = CurseForgeCurrentModPage.ConvertHtmlToFlowDocument(modDetails.Description);

                    // If the conversion was successful, set the document to the RichTextBox
                    if (flowDoc != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            descriptionRichTextBox.Document = flowDoc;
                        });
                    }
                }
                else
                {
                    // Handle error for description request
                }
            }
            else
            {
                // Handle error for mod details request
            }
        }

    }

}

public class ModResponse
{
    public Mod Data { get; set; }
}



public class Mod
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }

    public Links Links { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public int Status { get; set; }
    public int DownloadCount { get; set; }
    public bool IsFeatured { get; set; }
    public int PrimaryCategoryId { get; set; }
    public List<Category> Categories { get; set; }
    public int ClassId { get; set; }
    public List<Author> Authors { get; set; }
    public Logo Logo { get; set; }
    public List<Screenshot> Screenshots { get; set; }
    // ... other properties like LatestFiles, etc.
}

public class Links
{
    public string WebsiteUrl { get; set; }
    public string WikiUrl { get; set; }
    public string IssuesUrl { get; set; }
    public string SourceUrl { get; set; }
}
public class Category
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public DateTime DateModified { get; set; }
    public bool IsClass { get; set; }
    public int ClassId { get; set; }
    public int ParentCategoryId { get; set; }
}

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
}

public class Logo
{
    public int Id { get; set; }
    public int ModId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Url { get; set; }
}

public class Screenshot
{
    public int Id { get; set; }
    public int ModId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Url { get; set; }
}