using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Services;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class CreateSchedulePage : Page
    {
        private string _currentServerProfileName;
        private readonly INavigationService _navigationService;
        private int _timePickerCount = 1;
        private const string DatabaseFileName = "schedules.db";

        public CreateSchedulePage(INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _currentServerProfileName = ReadCurrentServerProfileNameFromJson();
            InitializeDatabase();
            LoadServers();
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
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
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentscheduleserver.json");
            if (File.Exists(path))
            {
                var jsonData = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                return data?["ServerProfileName"];
            }
            return null;
        }

        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            _timePickerCount++;
            var timePicker = new Xceed.Wpf.Toolkit.TimePicker
            {
                Name = $"timePicker{_timePickerCount}",
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 22,
                Width = 100,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var listBoxItem = new ListBoxItem();
            listBoxItem.Content = timePicker;
            timePickerListBox.Items.Add(listBoxItem);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schedule = CollectScheduleData();
                SaveScheduleToDatabase(schedule);

                MessageBox.Show("Schedule saved successfully!");

                ClearFormFields();

                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving schedule: {ex.Message}");
            }
        }

        private void ClearFormFields()
        {
            nicknameTextBox.Clear();
            actionComboBox.SelectedIndex = -1;
            rconCommandTextBox.Clear();
            timePickerListBox.Items.Clear();
            reoccurrenceIntervalTypeComboBox.SelectedIndex = -1;
            reoccurrenceIntervalTextBox.Clear();
            serverComboBox.SelectedIndex = -1;
            _currentServerProfileName = null;

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
            public List<TimeSpan> Times { get; set; } = new List<TimeSpan>();
            public List<string> Days { get; set; }
            public string ReoccurrenceIntervalType { get; set; }
            public int ReoccurrenceInterval { get; set; }
            public string Server { get; set; }
        }

        private Schedule CollectScheduleData()
        {
            if (string.IsNullOrEmpty(_currentServerProfileName))
            {
                _currentServerProfileName = ReadCurrentServerProfileNameFromJson();
                if (string.IsNullOrEmpty(_currentServerProfileName))
                {
                    throw new InvalidOperationException("Server profile name is not available.");
                }
            }

            var schedule = new Schedule
            {
                Nickname = nicknameTextBox?.Text,
                Action = (actionComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString(),
                RconCommand = rconCommandTextBox?.Visibility == Visibility.Visible ? rconCommandTextBox.Text : null,
                Days = new List<string>(),
                ReoccurrenceIntervalType = (reoccurrenceIntervalTypeComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString(),
                ReoccurrenceInterval = int.TryParse(reoccurrenceIntervalTextBox.Text, out int interval) ? interval : 0,
                Server = (serverComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
            };

            foreach (var item in timePickerListBox.Items)
            {
                if (item is ListBoxItem listBoxItem && listBoxItem.Content is Xceed.Wpf.Toolkit.TimePicker timePicker && timePicker.Value.HasValue)
                {
                    schedule.Times.Add(timePicker.Value.Value.TimeOfDay);
                }
            }

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

            ValidateSchedule(schedule);

            return schedule;
        }

        private void ValidateSchedule(Schedule schedule)
        {
            if (string.IsNullOrEmpty(schedule.Nickname))
                throw new InvalidOperationException("Nickname is required.");

            if (string.IsNullOrEmpty(schedule.Action))
                throw new InvalidOperationException("Action is required.");

            if (!schedule.Times.Any())
                throw new InvalidOperationException("At least one time must be selected.");

            if (!schedule.Days.Any())
                throw new InvalidOperationException("At least one day must be selected.");

            if (string.IsNullOrEmpty(schedule.Server))
                throw new InvalidOperationException("Server is required.");
        }

        private void InitializeDatabase()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            string dbPath = Path.Combine(folderPath, DatabaseFileName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Schedules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nickname TEXT,
                    Action TEXT,
                    RconCommand TEXT,
                    Times TEXT,
                    Days TEXT,
                    ReoccurrenceIntervalType TEXT,
                    ReoccurrenceInterval INTEGER,
                    Server TEXT
                )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void SaveScheduleToDatabase(Schedule schedule)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            string dbPath = Path.Combine(folderPath, DatabaseFileName);

            string times = JsonConvert.SerializeObject(schedule.Times);
            string days = JsonConvert.SerializeObject(schedule.Days);

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string insertQuery = @"
        INSERT INTO Schedules (
            Nickname, Action, RconCommand, Times, Days, ReoccurrenceIntervalType, ReoccurrenceInterval, Server
        ) VALUES (
            @Nickname, @Action, @RconCommand, @Times, @Days, @ReoccurrenceIntervalType, @ReoccurrenceInterval, @Server
        )";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Nickname", schedule.Nickname);
                    command.Parameters.AddWithValue("@Action", schedule.Action);
                    command.Parameters.AddWithValue("@RconCommand", schedule.RconCommand);
                    command.Parameters.AddWithValue("@Times", times);
                    command.Parameters.AddWithValue("@Days", days);
                    command.Parameters.AddWithValue("@ReoccurrenceIntervalType", schedule.ReoccurrenceIntervalType);
                    command.Parameters.AddWithValue("@ReoccurrenceInterval", schedule.ReoccurrenceInterval);
                    command.Parameters.AddWithValue("@Server", schedule.Server);
                    command.ExecuteNonQuery();
                }
            }
        }


        private void LoadServers()
        {
            try
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string serversFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");

                if (!File.Exists(serversFilePath))
                {
                    Debug.WriteLine("servers.json file not found.");
                    return;
                }

                string serversJson = File.ReadAllText(serversFilePath);
                Debug.WriteLine($"servers.json content: {serversJson}");

                var servers = JsonConvert.DeserializeObject<List<Server>>(serversJson);

                if (servers == null || !servers.Any())
                {
                    Debug.WriteLine("No servers loaded or failed to deserialize servers.json.");
                    return;
                }

                foreach (var server in servers)
                {
                    var comboBoxItem = new ComboBoxItem { Content = server.ProfileName };
                    serverComboBox.Items.Add(comboBoxItem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading servers: {ex.Message}");
            }
        }

        public class Server
        {
            public string ProfileName { get; set; }
            public string ServerPath { get; set; }
        }
    }
}
