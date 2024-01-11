using Ark_Ascended_Manager.Helpers;
using Ark_Ascended_Manager.Views.Pages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Ark_Ascended_Manager.Views.Pages
{

    public partial class CurseForgeCurrentModPage : Page
    {
        private readonly int _modId;

        public CurseForgeCurrentModPage(int modId)
        {
            InitializeComponent();
            _modId = modId;
            LoadModDetails();
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
                    HtmlToXamlConverter converter = new HtmlToXamlConverter();
                    FlowDocument document = converter.Convert(modDetails.Description, null, null, null) as FlowDocument;
                    descriptionRichTextBox.Document = document;
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