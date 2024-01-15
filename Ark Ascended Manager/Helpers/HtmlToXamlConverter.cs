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

            // Replace line breaks, paragraph, and header tags with XAML equivalents
            html = html.Replace("<br>", "<LineBreak/>")
                       .Replace("<br/>", "<LineBreak/>")
                       .Replace("<br />", "<LineBreak/>")
                       .Replace("<p>", "<Paragraph>")
                       .Replace("</p>", "</Paragraph>");

            // Convert HTML headers to XAML
            html = Regex.Replace(html, @"<h[1-5]>(.*?)<\/h[1-5]>", "<Paragraph><Bold>$1</Bold></Paragraph>");

            // Convert <strong> tags to <Bold>
            html = Regex.Replace(html, @"<strong>(.*?)<\/strong>", "<Bold>$1</Bold>");

            // Convert HTML hyperlinks to XAML Hyperlinks
            html = Regex.Replace(html, @"<a\s+href=['""]?([^'""]*)['""]?[^>]*>(.*?)<\/a>", "<Hyperlink NavigateUri=\"$1\">$2</Hyperlink>");

            // Convert HTML lists to XAML lists
            html = Regex.Replace(html, @"<ul>(.*?)<\/ul>", "<List MarkerStyle=\"Disc\">$1</List>", RegexOptions.Singleline);
            html = Regex.Replace(html, @"<li>(.*?)<\/li>", "<ListItem><Paragraph>$1</Paragraph></ListItem>", RegexOptions.Singleline);

            // Wrap plain text in a Paragraph if it's not already in one
            if (!Regex.IsMatch(html, @"<\s*(Paragraph|List)[^>]*>", RegexOptions.IgnoreCase))
            {
                html = $"<Paragraph>{html}</Paragraph>";
            }

            // Wrap in a FlowDocument tag if not already wrapped
            if (!html.TrimStart().StartsWith("<FlowDocument"))
            {
                html = $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{html}</FlowDocument>";
            }

            return html;
        }













        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
