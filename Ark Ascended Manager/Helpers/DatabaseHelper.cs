using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using Ark_Ascended_Manager.Views.Pages;

namespace Ark_Ascended_Manager.Helpers
{
    internal class DatabaseHelper
    {
        private const string DatabaseName = "Plugins.db";

        public DatabaseHelper()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={DatabaseName}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Plugins (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AltSupportURL TEXT,
                        IconURL TEXT,
                        Title TEXT,
                        ResourceID TEXT UNIQUE,
                        Version TEXT,
                        RatingAvg REAL,
                        Price TEXT,
                        TagLine TEXT,
                        ExternalURL TEXT,
                        ViewURL TEXT,
                        Tags TEXT,
                        UserID INTEGER,
                        Username TEXT,
                        ViewCount INTEGER,
                        DownloadCount INTEGER,
                        RatingCount INTEGER,
                        RatingWeighted REAL,
                        ResourceCategoryID INTEGER,
                        ResourceDate INTEGER,
                        ResourceState TEXT,
                        ResourceType TEXT
                    )
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine("Database initialized successfully.");
            }
        }

        public void SavePlugins(ObservableCollection<Resource> plugins)
        {
            using (var connection = new SqliteConnection($"Data Source={DatabaseName}"))
            {
                connection.Open();
                Debug.WriteLine($"Saving {plugins.Count} plugins to the database.");

                foreach (var plugin in plugins)
                {
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText =
                        @"
                            INSERT OR REPLACE INTO Plugins (AltSupportURL, IconURL, Title, ResourceID, Version, RatingAvg, Price, TagLine, ExternalURL, ViewURL, Tags, UserID, Username, ViewCount, DownloadCount, RatingCount, RatingWeighted, ResourceCategoryID, ResourceDate, ResourceState, ResourceType)
                            VALUES ($AltSupportURL, $IconURL, $Title, $ResourceID, $Version, $RatingAvg, $Price, $TagLine, $ExternalURL, $ViewURL, $Tags, $UserID, $Username, $ViewCount, $DownloadCount, $RatingCount, $RatingWeighted, $ResourceCategoryID, $ResourceDate, $ResourceState, $ResourceType)
                        ";

                        command.Parameters.AddWithValue("$AltSupportURL", plugin.AltSupportURL ?? string.Empty);
                        command.Parameters.AddWithValue("$IconURL", plugin.IconURL ?? string.Empty);
                        command.Parameters.AddWithValue("$Title", plugin.Title ?? string.Empty);
                        command.Parameters.AddWithValue("$ResourceID", plugin.ResourceID ?? string.Empty);
                        command.Parameters.AddWithValue("$Version", plugin.Version ?? string.Empty);
                        command.Parameters.AddWithValue("$RatingAvg", plugin.RatingAvg);
                        command.Parameters.AddWithValue("$Price", plugin.Price ?? string.Empty);
                        command.Parameters.AddWithValue("$TagLine", plugin.TagLine ?? string.Empty);
                        command.Parameters.AddWithValue("$ExternalURL", plugin.ExternalURL ?? string.Empty);
                        command.Parameters.AddWithValue("$ViewURL", plugin.ViewURL ?? string.Empty);
                        command.Parameters.AddWithValue("$Tags", string.Join(",", plugin.Tags ?? new List<string>()));
                        command.Parameters.AddWithValue("$UserID", plugin.UserID);
                        command.Parameters.AddWithValue("$Username", plugin.Username ?? string.Empty);
                        command.Parameters.AddWithValue("$ViewCount", plugin.ViewCount);
                        command.Parameters.AddWithValue("$DownloadCount", plugin.DownloadCount);
                        command.Parameters.AddWithValue("$RatingCount", plugin.RatingCount);
                        command.Parameters.AddWithValue("$RatingWeighted", plugin.RatingWeighted);
                        command.Parameters.AddWithValue("$ResourceCategoryID", plugin.ResourceCategoryID);
                        command.Parameters.AddWithValue("$ResourceDate", plugin.ResourceDate);
                        command.Parameters.AddWithValue("$ResourceState", plugin.ResourceState ?? string.Empty);
                        command.Parameters.AddWithValue("$ResourceType", plugin.ResourceType ?? string.Empty);

                        Debug.WriteLine($"Saving Plugin: {plugin.Title}");
                        command.ExecuteNonQuery();
                        Debug.WriteLine($"Saved Plugin: {plugin.Title}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to save plugin: {plugin.Title}, Error: {ex.Message}");
                    }
                }
            }
        }

        public ObservableCollection<Resource> LoadPlugins()
        {
            var plugins = new ObservableCollection<Resource>();

            using (var connection = new SqliteConnection($"Data Source={DatabaseName}"))
            {
                connection.Open();
                Debug.WriteLine("Loading plugins from the database.");

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Plugins";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var plugin = new Resource
                        {
                            AltSupportURL = reader.GetString(1),
                            IconURL = reader.GetString(2),
                            Title = reader.GetString(3),
                            ResourceID = reader.GetString(4),
                            Version = reader.GetString(5),
                            RatingAvg = reader.GetDouble(6),
                            Price = reader.GetString(7),
                            TagLine = reader.GetString(8),
                            ExternalURL = reader.GetString(9),
                            ViewURL = reader.GetString(10),
                            Tags = reader.GetString(11).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                            UserID = reader.GetInt32(12),
                            Username = reader.GetString(13),
                            ViewCount = reader.GetInt32(14),
                            DownloadCount = reader.GetInt32(15),
                            RatingCount = reader.GetInt32(16),
                            RatingWeighted = reader.GetDouble(17),
                            ResourceCategoryID = reader.GetInt32(18),
                            ResourceDate = reader.GetInt64(19),
                            ResourceState = reader.GetString(20),
                            ResourceType = reader.GetString(21)
                        };
                        plugins.Add(plugin);
                        Debug.WriteLine($"Loaded Plugin: {plugin.Title}");
                    }
                }
            }

            return plugins;
        }
    }
}
