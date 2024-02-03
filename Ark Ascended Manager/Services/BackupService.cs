using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Newtonsoft.Json;

namespace Ark_Ascended_Manager.Services
{
    internal class BackupService
    {
        private List<Server> servers;
        private List<FileSystemWatcher> watchers;
        private System.Timers.Timer refreshTimer;

        public BackupService()
        {
            servers = new List<Server>();
            watchers = new List<FileSystemWatcher>();
            LoadServers();
            SetupFileWatchers();
            InitializeRefreshTimer();
        }
        

        private void LoadServers()
        {
            Debug.WriteLine("Loading servers...");
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string serversFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");

            if (File.Exists(serversFilePath))
            {
                string serversJson = File.ReadAllText(serversFilePath);
                servers = JsonConvert.DeserializeObject<List<Server>>(serversJson);
                Debug.WriteLine($"Loaded {servers.Count} servers from {serversFilePath}.");
            }
            else
            {
                Debug.WriteLine($"Servers file not found at path: {serversFilePath}");
            }
        }

        private void SetupFileWatchers()
        {
            Debug.WriteLine("Setting up file watchers...");
            watchers.ForEach(watcher => watcher.Dispose());
            watchers.Clear();

            foreach (var server in servers)
            {
                string saveFilePath = Path.Combine(server.ServerPath, "ShooterGame", "Saved", "SavedArks", "TheIsland_WP", "TheIsland_WP.ark");
                if (File.Exists(saveFilePath))
                {
                    var watcher = new FileSystemWatcher
                    {
                        Path = Path.GetDirectoryName(saveFilePath),
                        Filter = Path.GetFileName(saveFilePath),
                        NotifyFilter = NotifyFilters.LastWrite
                    };

                    watcher.Changed += OnSaveFileChanged;
                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                    Debug.WriteLine($"File watcher set for {saveFilePath}");
                }
                else
                {
                    Debug.WriteLine($"Save file not found for server at path: {saveFilePath}");
                    return;
                    Debug.WriteLine($"Save file not found for server at path: {saveFilePath}");
                }
            }
        }


        private void OnSaveFileChanged(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"Detected change in file: {e.FullPath}");
            string backupFileName = $"{Path.GetFileNameWithoutExtension(e.Name)}_{DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss")}{Path.GetExtension(e.Name)}";
            string backupFilePath = Path.Combine(Path.GetDirectoryName(e.FullPath), backupFileName);

            try
            {
                File.Copy(e.FullPath, backupFilePath, true);
                Debug.WriteLine($"Backup created: {backupFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating backup: {ex.Message}");
            }
        }

        private void InitializeRefreshTimer()
        {
            Debug.WriteLine("Initializing refresh timer...");
            refreshTimer = new System.Timers.Timer(300000); // Refresh every 5 minutes (300000 ms)
            refreshTimer.Elapsed += OnRefreshTimerElapsed;
            refreshTimer.AutoReset = true;
            refreshTimer.Start();
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Refresh timer elapsed. Reloading servers and setting up watchers again...");
            LoadServers();
            SetupFileWatchers(); // Re-setup file watchers for new servers
        }
    }

    public class server
    {
        // Define properties as in your JSON structure
        public string ServerPath { get; set; }
        // ... other properties ...
    }
}
