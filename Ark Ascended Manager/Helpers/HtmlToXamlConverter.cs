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
                FlowDocument document = XamlReader.Parse(xamlContent) as FlowDocument;

                return document;
            }
            else
            {
                // If the content is not a string, return an empty FlowDocument.
                return new FlowDocument();
            }
        }

        public string ConvertHtmlToXaml(string html)
        {
            // Decode any HTML entities that are encoded in the HTML.
            html = System.Net.WebUtility.HtmlDecode(html);

            // Replace line breaks with XAML line break Run elements.
            html = html.Replace("<br>", "<LineBreak/>")
                       .Replace("<br/>", "<LineBreak/>")
                       .Replace("<br />", "<LineBreak/>");

            // Replace paragraph tags with XAML Paragraph elements.
            html = html.Replace("<p>", "<Paragraph>")
                       .Replace("</p>", "</Paragraph>");

            // Debug: Print the HTML content after replacing tags.
            Console.WriteLine("HTML after tag replacements:");
            Console.WriteLine(html);

            // Identify and replace links with Hyperlink elements.
            html = Regex.Replace(html, @"<a\s+(?:[^>]*?\s+)?href=['""]([^'""]*)['""][^>]*?>(.*?)</a>",
                match =>
                {
                    string url = match.Groups[1].Value;
                    string linkText = match.Groups[2].Value;
                    return $"<Hyperlink NavigateUri=\"{url}\">{linkText}</Hyperlink>";
                });

            // Debug: Print the HTML content after adding Hyperlink elements.
            Console.WriteLine("HTML after adding Hyperlink elements:");
            Console.WriteLine(html);

            // Remove JSON data
            string jsonData = "{\"data\":";
            if (html.StartsWith(jsonData))
            {
                html = html.Remove(0, jsonData.Length);
                if (html.EndsWith("}"))
                {
                    html = html.Remove(html.Length - 1);
                }
            }

            // Debug: Print the HTML content after removing JSON data.
            Console.WriteLine("HTML after removing JSON data:");
            Console.WriteLine(html);

            // Wrap the HTML content in a Paragraph tag to be used as FlowDocument content.
            html = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                   "<Paragraph>" + html + "</Paragraph></FlowDocument>";

            return html;
        }






        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
