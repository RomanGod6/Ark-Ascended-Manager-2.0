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
            Logger.Log("Loading servers...");
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string serversFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");

            if (File.Exists(serversFilePath))
            {
                string serversJson = File.ReadAllText(serversFilePath);
                servers = JsonConvert.DeserializeObject<List<Server>>(serversJson);
                Logger.Log($"Loaded {servers.Count} servers from {serversFilePath}.");
            }
            else
            {
                Logger.Log($"Servers file not found at path: {serversFilePath}");
            }
        }

        private void SetupFileWatchers()
        {
            try
            {
                if (watchers == null)
                {
                    Logger.Log("Watchers list is null.");
                    return;
                }

                Logger.Log("Setting up file watchers...");

                // Dispose existing watchers
                foreach (var watcher in watchers)
                {
                    watcher.Dispose();
                }
                watchers.Clear();

                // Ensure servers collection is not null
                if (servers == null)
                {
                    Logger.Log("Servers collection is null.");
                    return;
                }

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
                        Logger.Log($"File watcher set for {saveFilePath}");
                    }
                    else
                    {
                        Logger.Log($"Save file not found for server at path: {saveFilePath}");
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"An error occurred while setting up file watchers: {ex.Message}");
                // Optionally, you can throw the exception further if you want to handle it at a higher level
                // throw;
            }
        }



        private void OnSaveFileChanged(object sender, FileSystemEventArgs e)
        {
            Logger.Log($"Detected change in file: {e.FullPath}");
            string backupFileName = $"{Path.GetFileNameWithoutExtension(e.Name)}_{DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss")}{Path.GetExtension(e.Name)}";
            string backupFilePath = Path.Combine(Path.GetDirectoryName(e.FullPath), backupFileName);

            try
            {
                File.Copy(e.FullPath, backupFilePath, true);
                Logger.Log($"Backup created: {backupFilePath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating backup: {ex.Message}");
            }
        }

        private void InitializeRefreshTimer()
        {
            Logger.Log("Initializing refresh timer...");
            refreshTimer = new System.Timers.Timer(300000); // Refresh every 5 minutes (300000 ms)
            refreshTimer.Elapsed += OnRefreshTimerElapsed;
            refreshTimer.AutoReset = true;
            refreshTimer.Start();
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Log("Refresh timer elapsed. Reloading servers and setting up watchers again...");
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
