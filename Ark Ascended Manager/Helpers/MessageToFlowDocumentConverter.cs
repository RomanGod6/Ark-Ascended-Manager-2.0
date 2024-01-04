using System;
using System.Globalization;
using System.Windows.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ark_Ascended_Manager.Helpers
{
    public class MessageToFlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value as string;
            if (string.IsNullOrEmpty(input)) return null;

            FlowDocument document = new FlowDocument();
            string[] lines = input.Split(new string[] { "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                Paragraph paragraph = new Paragraph();
                int currentIndex = 0;

                while (currentIndex < line.Length)
                {
                    int tagStart = line.IndexOf("<RichColor", currentIndex);
                    if (tagStart == -1)
                    {
                        paragraph.Inlines.Add(new Run(line.Substring(currentIndex)));
                        break;
                    }
                    else
                    {
                        if (tagStart > currentIndex)
                        {
                            paragraph.Inlines.Add(new Run(line.Substring(currentIndex, tagStart - currentIndex)));
                        }

                        int tagEnd = line.IndexOf(">", tagStart);
                        if (tagEnd == -1)
                        {
                            // Invalid tag, add as plain text
                            paragraph.Inlines.Add(new Run(line.Substring(tagStart)));
                            break;
                        }

                        string tag = line.Substring(tagStart, tagEnd - tagStart + 1);
                        Color color = ParseColorFromTag(tag);

                        int textStart = tagEnd + 1;
                        int textEnd = line.IndexOf("</>", textStart);
                        if (textEnd == -1)
                        {
                            textEnd = line.Length;
                        }

                        string text = line.Substring(textStart, textEnd - textStart);
                        Run textRun = new Run(text)
                        {
                            Foreground = new SolidColorBrush(color)
                        };
                        paragraph.Inlines.Add(textRun);

                        currentIndex = textEnd + 3;
                    }
                }

                document.Blocks.Add(paragraph);
            }

            return document;
        }

        private Color ParseColorFromTag(string tag)
        {
            // Find the start and end index of the color value inside the tag
            int startIndex = tag.IndexOf('"') + 1;
            int endIndex = tag.LastIndexOf('"');
            if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex)
            {
                return Colors.Black; // Return a default color if parsing fails
            }

            // Extract the color value string
            string colorValue = tag.Substring(startIndex, endIndex - startIndex);

            // Split the color value string into its components
            string[] components = colorValue.Split(',');

            // Parse the RGBA components
            // Assume each component is a float number between 0 and 1
            if (components.Length == 4)
            {
                if (float.TryParse(components[0], out float r) &&
                    float.TryParse(components[1], out float g) &&
                    float.TryParse(components[2], out float b) &&
                    float.TryParse(components[3], out float a))
                {
                    return Color.FromArgb((byte)(a * 255), (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
                }
            }

            return Colors.Black; // Return a default color if parsing fails
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This part is usually not needed for this kind of conversion
            throw new NotImplementedException();
        }
    }
}
