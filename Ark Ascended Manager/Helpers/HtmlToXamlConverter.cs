using System;
using System.Windows.Data;
using System.Windows.Documents;
using System.Text.RegularExpressions;
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

            // Check if the extracted property is indeed a string and replace the <br> tags.
            if (htmlContent is string content)
            {
                content = content.Replace("<br>", "\n");
                // Now you should convert this content into a FlowDocument or other suitable format for displaying in a RichTextBox.
                // Since we don't have the full logic of how you're converting HTML to XAML,
                // Let's assume you're just going to wrap it in a Run element for simplicity.
                FlowDocument document = new FlowDocument(new Paragraph(new Run(content)));

                // The result is the FlowDocument we created, which can be set to the RichTextBox's Document property.
                return document;
            }
            else
            {
                // If the content is not a string, you need to decide what to do.
                // Perhaps return an empty FlowDocument or handle the error differently.
                return new FlowDocument();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}