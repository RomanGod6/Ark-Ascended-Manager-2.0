using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Services;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class CreateSchedulePage : Page
    {
        private string _currentServerProfileName;
        private readonly INavigationService _navigationService;
        private int _timePickerCount = 1;
        private readonly string _databaseFilePath;
        private int? _editScheduleId = null; // Track the schedule ID being edited

        private ObservableCollection<Schedule> _schedules;
        private ICollectionView _schedulesView;
        private int _currentPage = 1;
        private const int _itemsPerPage = 10;

        public CreateSchedulePage(INavigationService navigationService)
        {
            InitializeComponent();
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _currentServerProfileName = ReadCurrentServerProfileNameFromJson();
            _databaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "Schedules.db");
            InitializeDatabase();
            LoadServers();
            LoadExistingSchedules(); // Load existing schedules into DataGrid
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Custom RCON Command")
                {
                    rconCommandTextBox.Visibility = Visibility.Visible;
                    reoccurrenceIntervalTypeComboBox.Visibility = Visibility.Visible;
                    reoccurrenceIntervalTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    rconCommandTextBox.Visibility = Visibility.Collapsed;
                    reoccurrenceIntervalTypeComboBox.Visibility = Visibility.Collapsed;
                    reoccurrenceIntervalTextBox.Visibility = Visibility.Collapsed;
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
                    var comboBoxItem = new ComboBoxItem { Content = server.ServerName };
                    serverComboBox.Items.Add(comboBoxItem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading servers: {ex.Message}");
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
                Style = (Style)FindResource("TimePickerStyle")
            };

            var listBoxItem = new ListBoxItem
            {
                Content = timePicker,
                Style = (Style)FindResource("ListBoxItemStyle")
            };

            timePickerListBox.Items.Add(listBoxItem);
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var schedule = CollectScheduleData();
                if (_editScheduleId.HasValue)
                {
                    UpdateScheduleInDatabase(schedule);
                }
                else
                {
                    SaveScheduleToDatabase(schedule);
                }

                MessageBox.Show("Schedule saved successfully!");

                ClearFormFields();

                LoadExistingSchedules(); // Reload schedules after saving
                _editScheduleId = null; // Clear the edit mode
                saveButton.Content = "Save"; // Reset the button content to Save
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

        public class Schedule : INotifyPropertyChanged
        {
            private bool _isNotCustomRconCommand;

            public int Id { get; set; }
            public string Nickname { get; set; }
            public string Action { get; set; }
            public string RconCommand { get; set; }
            public string Times { get; set; }
            public string Days { get; set; }
            public string ReoccurrenceIntervalType { get; set; }
            public int ReoccurrenceInterval { get; set; }
            public string Server { get; set; }

            public bool IsNotCustomRconCommand
            {
                get => _isNotCustomRconCommand;
                set
                {
                    _isNotCustomRconCommand = value;
                    OnPropertyChanged(nameof(IsNotCustomRconCommand));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
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
                Id = _editScheduleId ?? 0, // Use the edit ID if available, otherwise 0
                Nickname = nicknameTextBox?.Text,
                Action = (actionComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString(),
                RconCommand = rconCommandTextBox?.Visibility == Visibility.Visible ? rconCommandTextBox.Text : null,
                Days = JsonConvert.SerializeObject(daysPanel.Children.OfType<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => cb.Content.ToString()).ToList()),
                Times = JsonConvert.SerializeObject(timePickerListBox.Items.OfType<ListBoxItem>().Select(item => (item.Content as Xceed.Wpf.Toolkit.TimePicker).Value.Value.TimeOfDay.ToString()).ToList()),
                ReoccurrenceIntervalType = (reoccurrenceIntervalTypeComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Server = (serverComboBox?.SelectedItem as ComboBoxItem)?.Content.ToString() // Correctly retrieve selected server
            };

            if (reoccurrenceIntervalTextBox.Visibility == Visibility.Visible && !string.IsNullOrEmpty(reoccurrenceIntervalTextBox.Text))
            {
                if (int.TryParse(reoccurrenceIntervalTextBox.Text, out int interval))
                {
                    schedule.ReoccurrenceInterval = interval;
                }
                else
                {
                    throw new InvalidOperationException("Reoccurrence interval must be a valid number.");
                }
            }

            schedule.IsNotCustomRconCommand = schedule.Action != "Custom RCON Command";

            return schedule;
        }


        private void SaveScheduleToDatabase(Schedule schedule)
        {
            using (var connection = new SQLiteConnection($"Data Source={_databaseFilePath};Version=3;"))
            {
                connection.Open();

                var commandText = @"INSERT INTO Schedules (Nickname, Action, RconCommand, Times, Days, ReoccurrenceIntervalType, ReoccurrenceInterval, Server)
                            VALUES (@Nickname, @Action, @RconCommand, @Times, @Days, @ReoccurrenceIntervalType, @ReoccurrenceInterval, @Server)";

                using (var command = new SQLiteCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@Nickname", schedule.Nickname);
                    command.Parameters.AddWithValue("@Action", schedule.Action);
                    command.Parameters.AddWithValue("@RconCommand", schedule.RconCommand);
                    command.Parameters.AddWithValue("@Times", schedule.Times);
                    command.Parameters.AddWithValue("@Days", schedule.Days);
                    command.Parameters.AddWithValue("@ReoccurrenceIntervalType", schedule.ReoccurrenceIntervalType);
                    command.Parameters.AddWithValue("@ReoccurrenceInterval", schedule.ReoccurrenceInterval);
                    command.Parameters.AddWithValue("@Server", schedule.Server); // Ensure server name is correctly saved

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }


        private void LoadExistingSchedules()
        {
            _schedules = new ObservableCollection<Schedule>();

            using (var connection = new SQLiteConnection($"Data Source={_databaseFilePath};Version=3;"))
            {
                connection.Open();

                var commandText = "SELECT Id, Nickname, Action, RconCommand, Times, Days, ReoccurrenceIntervalType, ReoccurrenceInterval, Server FROM Schedules";

                using (var command = new SQLiteCommand(commandText, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var schedule = new Schedule
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nickname = reader["Nickname"].ToString(),
                                Action = reader["Action"].ToString(),
                                RconCommand = reader["RconCommand"].ToString(),
                                Times = reader["Times"].ToString(),
                                Days = reader["Days"].ToString(),
                                ReoccurrenceIntervalType = reader["ReoccurrenceIntervalType"].ToString(),
                                ReoccurrenceInterval = Convert.ToInt32(reader["ReoccurrenceInterval"]),
                                Server = reader["Server"].ToString(),
                                IsNotCustomRconCommand = reader["Action"].ToString() != "Custom RCON Command"
                            };
                            _schedules.Add(schedule);
                        }
                    }
                }

                connection.Close();
            }

            _schedulesView = CollectionViewSource.GetDefaultView(_schedules);
            schedulesDataGrid.ItemsSource = _schedulesView;
            UpdatePagination();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_schedulesView != null)
            {
                _schedulesView.Filter = item =>
                {
                    if (item is Schedule schedule)
                    {
                        return string.IsNullOrWhiteSpace(searchTextBox.Text) ||
                               schedule.Nickname.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                               schedule.Action.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                               schedule.Server.Contains(searchTextBox.Text, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                };
                _schedulesView.Refresh();
                UpdatePagination();
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < (_schedules.Count + _itemsPerPage - 1) / _itemsPerPage)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

        private void UpdatePagination()
        {
            if (_schedulesView != null)
            {
                _schedulesView.Filter = item =>
                {
                    int index = _schedules.IndexOf(item as Schedule);
                    return index >= (_currentPage - 1) * _itemsPerPage && index < _currentPage * _itemsPerPage;
                };
                _schedulesView.Refresh();
                pageNumberTextBlock.Text = $"Page {_currentPage}";
            }
        }

        private void DeleteScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            if (schedulesDataGrid.SelectedItem != null)
            {
                var selectedItem = schedulesDataGrid.SelectedItem as Schedule;
                var nickname = selectedItem.Nickname;

                if (MessageBox.Show($"Are you sure you want to delete schedule '{nickname}'?", "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DeleteScheduleFromDatabase(nickname);
                    LoadExistingSchedules(); // Reload schedules after deleting
                }
            }
            else
            {
                MessageBox.Show("Please select a schedule to delete.");
            }
        }

        private void DeleteScheduleFromDatabase(string nickname)
        {
            using (var connection = new SQLiteConnection($"Data Source={_databaseFilePath};Version=3;"))
            {
                connection.Open();

                var commandText = "DELETE FROM Schedules WHERE Nickname = @Nickname";

                using (var command = new SQLiteCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@Nickname", nickname);
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (schedulesDataGrid.SelectedItem != null)
            {
                var selectedItem = schedulesDataGrid.SelectedItem as Schedule;
                UpdateScheduleInDatabase(selectedItem);
                LoadExistingSchedules(); // Reload schedules after updating
            }
            else
            {
                MessageBox.Show("Please select a schedule to update.");
            }
        }

        private void UpdateScheduleInDatabase(Schedule schedule)
        {
            using (var connection = new SQLiteConnection($"Data Source={_databaseFilePath};Version=3;"))
            {
                connection.Open();

                var commandText = @"UPDATE Schedules 
                                    SET Nickname = @Nickname, Action = @Action, RconCommand = @RconCommand, Times = @Times, Days = @Days, 
                                    ReoccurrenceIntervalType = @ReoccurrenceIntervalType, ReoccurrenceInterval = @ReoccurrenceInterval, Server = @Server
                                    WHERE Id = @Id";

                using (var command = new SQLiteCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@Nickname", schedule.Nickname);
                    command.Parameters.AddWithValue("@Action", schedule.Action);
                    command.Parameters.AddWithValue("@RconCommand", schedule.RconCommand);
                    command.Parameters.AddWithValue("@Times", schedule.Times);
                    command.Parameters.AddWithValue("@Days", schedule.Days);
                    command.Parameters.AddWithValue("@ReoccurrenceIntervalType", schedule.ReoccurrenceIntervalType);
                    command.Parameters.AddWithValue("@ReoccurrenceInterval", schedule.ReoccurrenceInterval);
                    command.Parameters.AddWithValue("@Server", schedule.Server);
                    command.Parameters.AddWithValue("@Id", schedule.Id);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void schedulesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var selectedSchedule = dataGrid.SelectedItem as Schedule;
            if (selectedSchedule != null)
            {
                UpdateScheduleInDatabase(selectedSchedule);
            }
        }

        private void InitializeDatabase()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(_databaseFilePath))
            {
                SQLiteConnection.CreateFile(_databaseFilePath);
            }

            using (var connection = new SQLiteConnection($"Data Source={_databaseFilePath};Version=3;"))
            {
                connection.Open();

                var commandText = @"CREATE TABLE IF NOT EXISTS Schedules (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Nickname TEXT NOT NULL,
                                    Action TEXT NOT NULL,
                                    RconCommand TEXT,
                                    Times TEXT NOT NULL,
                                    Days TEXT NOT NULL,
                                    ReoccurrenceIntervalType TEXT,
                                    ReoccurrenceInterval INTEGER,
                                    Server TEXT NOT NULL)";

                using (var command = new SQLiteCommand(commandText, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
{
    if (schedulesDataGrid.SelectedItem is Schedule selectedSchedule)
    {
        _editScheduleId = selectedSchedule.Id;
        nicknameTextBox.Text = selectedSchedule.Nickname;
        actionComboBox.SelectedItem = actionComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedSchedule.Action);
        rconCommandTextBox.Text = selectedSchedule.RconCommand;

        // Debugging for server selection
        Debug.WriteLine("Available servers in ComboBox:");
        foreach (var item in serverComboBox.Items)
        {
            Debug.WriteLine((item as ComboBoxItem).Content.ToString());
        }
        Debug.WriteLine($"Attempting to select server: {selectedSchedule.Server}");

        // Set server selection
        var serverItem = serverComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => (item.Content as string) == selectedSchedule.Server);
        if (serverItem != null)
        {
            serverComboBox.SelectedItem = serverItem;
        }
        else
        {
            Debug.WriteLine($"Server '{selectedSchedule.Server}' not found in ComboBox.");
        }

        // Set times
        var times = JsonConvert.DeserializeObject<List<string>>(selectedSchedule.Times);
        timePickerListBox.Items.Clear();
        foreach (var time in times)
        {
            var timePicker = new Xceed.Wpf.Toolkit.TimePicker
            {
                Value = DateTime.Today.Add(TimeSpan.Parse(time)), // Convert TimeSpan to DateTime
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 22,
                Width = 100,
                Margin = new Thickness(0, 5, 0, 0)
            };
            var listBoxItem = new ListBoxItem { Content = timePicker };
            timePickerListBox.Items.Add(listBoxItem);
        }

        // Set days
        var days = JsonConvert.DeserializeObject<List<string>>(selectedSchedule.Days);
        foreach (var control in daysPanel.Children)
        {
            if (control is CheckBox checkBox)
            {
                checkBox.IsChecked = days.Contains(checkBox.Content.ToString());
            }
        }

        reoccurrenceIntervalTypeComboBox.SelectedItem = reoccurrenceIntervalTypeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedSchedule.ReoccurrenceIntervalType);
        reoccurrenceIntervalTextBox.Text = selectedSchedule.ReoccurrenceInterval.ToString();

        saveButton.Content = "Update";
    }
}



    }
}
