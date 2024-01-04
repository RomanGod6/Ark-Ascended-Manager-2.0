using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Wpf.Ui.Services;
using static Ark_Ascended_Manager.Views.Pages.ConfigPage;

namespace Ark_Ascended_Manager.Views.Pages
{
    /// <summary>
    /// Interaction logic for CreateSchedulePage.xaml
    /// </summary>
    public partial class CreateSchedulePage : Page
    {
        private string _currentServerProfileName;
        private readonly INavigationService _navigationService;

       

        public CreateSchedulePage(INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _currentServerProfileName = ReadCurrentServerProfileNameFromJson();
        }
        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Check if the selected item is "Custom RCON Command"
                if (selectedItem.Content.ToString() == "Custom RCON Command")
                {
                    rconCommandTextBox.Visibility = Visibility.Visible;
                    reoccursEveryPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    rconCommandTextBox.Visibility = Visibility.Collapsed;
                    reoccursEveryPanel.Visibility = Visibility.Collapsed;
                }
            }
        }
        private string ReadCurrentServerProfileNameFromJson()
        {
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentscheduleserver.json");
            if (File.Exists(path))
            {
                var jsonData = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                return data?["ServerProfileName"];
            }
            return null;
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ReadCurrentServerProfileNameFromJson();
            var schedule = CollectScheduleData();
            var json = SerializeScheduleToJson(schedule);
            
            SaveScheduleToJson(json);

            MessageBox.Show("Schedule saved successfully!");

            // Clear the form fields (if needed)
            ClearFormFields();

            // Re-read the current server profile name from the JSON file for the next entry
           

            // Navigate back or refresh the page
            _navigationService.GoBack(); // Or use Navigate(typeof(ServerPage)) if you're not using a stack-based navigation
        }
        private void ClearFormFields()
        {
            nicknameTextBox.Clear();
            actionComboBox.SelectedIndex = -1; // Reset to no selection
            rconCommandTextBox.Clear();
            timePicker.Value = null; // Reset to no value
            reoccurrenceIntervalTypeComboBox.SelectedIndex = -1; // Reset to no selection
            reoccurrenceIntervalTextBox.Clear();
            _currentServerProfileName = null;

            // Uncheck all days checkboxes
            foreach (var control in daysPanel.Children)
            {
                if (control is CheckBox checkBox)
                {
                    checkBox.IsChecked = false;
                }
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            _navigationService.GoBack();
        }
       
        public class Schedule
        {
            public string Nickname { get; set; }
            public string Action { get; set; }
            public string RconCommand { get; set; }
            public TimeSpan Time { get; set; }
            public List<string> Days { get; set; }
            public string ReoccurrenceIntervalType { get; set; }
            public int ReoccurrenceInterval { get; set; }
            public string Server { get; set; } // You need to determine how to get this information
        }
        private Schedule CollectScheduleData()
        {
            // Check if _currentServerProfileName is null or empty, if so, attempt to read it again
            if (string.IsNullOrEmpty(_currentServerProfileName))
            {
                _currentServerProfileName = ReadCurrentServerProfileNameFromJson();
                // If it's still null or empty after the attempt, you can handle it accordingly
                if (string.IsNullOrEmpty(_currentServerProfileName))
                {
                    // You can throw an exception, set a default value, or handle it in another way
                    throw new InvalidOperationException("Server profile name is not available.");
                }
            }

            var schedule = new Schedule
            {
                Nickname = nicknameTextBox?.Text,
                Action = (actionComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString(),
                RconCommand = rconCommandTextBox?.Visibility == Visibility.Visible ? rconCommandTextBox.Text : null,
                Time = timePicker?.Value.HasValue == true ? timePicker.Value.Value.TimeOfDay : TimeSpan.Zero,
                Days = new List<string>(),
                ReoccurrenceIntervalType = (reoccurrenceIntervalTypeComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString(),
                ReoccurrenceInterval = int.TryParse(reoccurrenceIntervalTextBox.Text, out int interval) ? interval : 0,
                Server = _currentServerProfileName
            };

            if (daysPanel != null)
            {
                foreach (var control in daysPanel.Children)
                {
                    if (control is CheckBox checkBox && checkBox.IsChecked == true)
                    {
                        schedule.Days.Add(checkBox.Content.ToString());
                    }
                }
            }

            return schedule;
        }



        private string SerializeScheduleToJson(Schedule schedule)
        {
            return JsonConvert.SerializeObject(schedule, Formatting.Indented);
        }
        private void SaveScheduleToJson(string json)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = System.IO.Path.Combine(folderPath, "Ark Ascended Manager");
            string fileName = "schedules.json";

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            string fullPath = System.IO.Path.Combine(appFolder, fileName);

            List<Schedule> schedules = new List<Schedule>();
            if (File.Exists(fullPath))
            {
                // Read the existing file and deserialize its content
                var existingJson = File.ReadAllText(fullPath);
                schedules = JsonConvert.DeserializeObject<List<Schedule>>(existingJson) ?? new List<Schedule>();
            }

            // Add the new schedule
            Schedule newSchedule = JsonConvert.DeserializeObject<Schedule>(json);
            schedules.Add(newSchedule);

            // Serialize the list of schedules and save it back to the file
            var updatedJson = JsonConvert.SerializeObject(schedules, Formatting.Indented);
            File.WriteAllText(fullPath, updatedJson);
        }




    }

}
