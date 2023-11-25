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
using System.Windows.Shapes;
using System.Text.Json;


namespace Ark_Ascended_Manager.Views.Pages
{
    /// <summary>
    /// Interaction logic for IssueReportForm.xaml
    /// </summary>
    public partial class IssueReportForm : Page
    {
        public IssueReportForm()
        {
            InitializeComponent();
        }
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string issueTitle = IssueTitle.Text;
            string discordUsername = DiscordUsername.Text;
            string issueDescription = IssueDescription.Text;
            string attachmentLink = AttachmentLink.Text;
            string additionalInfo = AdditionalInformation.Text;

            // Validate input, e.g., check if fields are not empty

            // Construct the message to send
            string message = $"**Issue Title:** {issueTitle}\n" +
                             $"**Discord Username:** {discordUsername}\n" +
                             $"**Description:** {issueDescription}\n" +
                             $"**Attachment:** {attachmentLink}\n" +
                             $"**Additional Information:** {additionalInfo}";

            // Send to Discord via webhook
            string webhookUrl = "https://discord.com/api/webhooks/1177498898277875753/EueCWRcSowxKQNgBoJZav9arPOGIGP0Ef7wNgx6gaZWSi0oJ6neN3ZHhiGh5Edfa9R2V"; // Replace with your actual webhook URL
            await SendToDiscordWebhook(webhookUrl, issueTitle, discordUsername, issueDescription, attachmentLink, additionalInfo);

            // Close the form or show a message
        }

        private async Task SendToDiscordWebhook(string webhookUrl, string issueTitle, string discordUsername, string issueDescription, string attachmentLink, string additionalInfo)
        {
            using var client = new HttpClient();
            var embed = new
            {
                embeds = new[]
                {
            new
            {
                title = issueTitle,
                description = issueDescription,
                fields = new[]
                {
                    new { name = "Discord Username", value = discordUsername },
                    new { name = "Attachment", value = attachmentLink },
                    new { name = "Additional Information", value = additionalInfo }
                },
                color = 5814783 // You can change the color of the embed here
            }
        }
            };

            string json = JsonSerializer.Serialize(embed);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, content);

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Your report has been sent successfully.", "Report Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("There was an error sending your report. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }


}
