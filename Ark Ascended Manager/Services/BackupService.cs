﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
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
                servers = JsonConvert.DeserializeObject<List<Server>>(serversJson) ?? new List<Server>();
                Logger.Log($"Loaded {servers.Count} servers from {serversFilePath}.");
            }
            else
            {
                Logger.Log($"Servers file not found at path: {serversFilePath}");
            }
        }

        private void SetupFileWatchers()
        {
            Logger.Log("Setting up file watchers...");

            // Dispose existing watchers
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }
            watchers.Clear();

            // Ensure servers collection is not null
            if (servers == null || !servers.Any())
            {
                Logger.Log("Servers collection is null or empty.");
                return;
            }

            foreach (var server in servers)
            {
                if (string.IsNullOrEmpty(server.ServerPath))
                {
                    Logger.Log($"ServerPath is null or empty for server: {server.ProfileName}");
                    continue;
                }

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

        private void OnSaveFileChanged(object sender, FileSystemEventArgs e)
        {
            Logger.Log($"Detected change in file: {e.FullPath}");
            string backupFileName = $"{Path.GetFileNameWithoutExtension(e.Name)}_{DateTime.Now:dd.MM.yyyy_HH.mm.ss}{Path.GetExtension(e.Name)}";
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

    public class Server
    {
        public string ProfileName { get; set; }
        public string ServerStatus { get; set; }
        public string ServerPath { get; set; }
        public string MapName { get; set; }
        public string AppId { get; set; }
        public string ServerIP { get; set; }
        public bool IsRunning { get; set; }
        public string ServerName { get; set; }
        public int ListenPort { get; set; }
        public int RCONPort { get; set; }
        public List<string> Mods { get; set; }
        public int MaxPlayerCount { get; set; }
        public string AdminPassword { get; set; }
        public string ServerIcon { get; set; }
        public string ServerPassword { get; set; }
        public bool UseBattlEye { get; set; }
        public bool ForceRespawnDinos { get; set; }
        public bool PreventSpawnAnimation { get; set; }
        public int ChangeNumber { get; set; }
        public string ChangeNumberStatus { get; set; }
        public bool IsServerRunning { get; set; }

        public bool UpdateOnRestart { get; set; }
    }

}
