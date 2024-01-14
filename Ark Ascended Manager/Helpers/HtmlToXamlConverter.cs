using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Helpers
{
    public class HtmlToXamlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string jsonString = (string)value; // Cast the value to a string.

            // Deserialize the JSON string to a dynamic object
            dynamic dynamicObject = JsonConvert.DeserializeObject<dynamic>(jsonString);

            string htmlContent = dynamicObject.someProperty; // Extract the property you need.

            // Check if the extracted property is indeed a string.
            if (htmlContent is string content)
            {
                // Convert the HTML content to XAML.
                string xamlContent = ConvertHtmlToXaml(content);

                // Parse the XAML into a FlowDocument.
                try
                {
                    FlowDocument document = XamlReader.Parse(xamlContent) as FlowDocument;
                    return document;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error converting HTML to XAML: " + ex.Message);
                    return new FlowDocument();
                }
            }
            else
            {
                // If the content is not a string, return an empty FlowDocument.
                return new FlowDocument();
            }
        }

        public string ConvertHtmlToXaml(string html)
        {
            // Decode HTML entities
            html = System.Net.WebUtility.HtmlDecode(html);

            // Replace line breaks and paragraph tags with XAML equivalents
            html = html.Replace("<br>", "<LineBreak/>")
                       .Replace("<br/>", "<LineBreak/>")
                       .Replace("<br />", "<LineBreak/>")
                       .Replace("<p>", "<Paragraph>")
                       .Replace("</p>", "</Paragraph>");

            // Handle <h1> tags by converting them to XAML headings
            html = Regex.Replace(html, @"<h1>(.*?)<\/h1>", "<Paragraph><Bold>$1</Bold></Paragraph>");

            // Handle <h2> tags by converting them to XAML headings
            html = Regex.Replace(html, @"<h2>(.*?)<\/h2>", "<Paragraph><Underline>$1</Underline></Paragraph>");

            // Remove HTML hyperlinks and URLs
            html = Regex.Replace(html, @"<a\s+href=['""]?([^'""]*)['""]?[^>]*>(.*?)<\/a>", ""); // Remove hyperlinks
            html = Regex.Replace(html, @"https?:\/\/[^\s]+", ""); // Remove plain URLs

            // Handle <ul> and <li> tags by converting them to XAML list elements
            html = Regex.Replace(html, @"<ul>(.*?)<\/ul>", "<List>$1</List>");
            html = Regex.Replace(html, @"<li>(.*?)<\/li>", "<ListItem>$1</ListItem>");

            // Remove newline characters
            html = html.Replace("\n", "");

            // Wrap in a FlowDocument tag
            html = $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{html}</FlowDocument>";

            return html;
        }










        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
