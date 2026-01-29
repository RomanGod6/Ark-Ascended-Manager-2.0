using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ark_Ascended_Manager.Models;

namespace Ark_Ascended_Manager.Services
{
    internal class BackupService
    {
        private System.Timers.Timer _backupCheckTimer;
        private readonly string _serversJsonPath;
        private bool _isBackupInProgress = false;

        public BackupService()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _serversJsonPath = Path.Combine(appDataFolder, "Ark Ascended Manager", "servers.json");

            InitializeBackupTimer();
            Logger.Log("BackupService initialized with manager-controlled backup system.");
        }

        private void InitializeBackupTimer()
        {
            // Check for due backups every 5 minutes
            _backupCheckTimer = new System.Timers.Timer(300000); // 5 minutes
            _backupCheckTimer.Elapsed += OnBackupCheckTimerElapsed;
            _backupCheckTimer.AutoReset = true;
            _backupCheckTimer.Start();
            Logger.Log("Backup check timer started (checks every 5 minutes).");
        }

        private async void OnBackupCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isBackupInProgress)
            {
                Logger.Log("Backup already in progress, skipping this check cycle.");
                return;
            }

            try
            {
                _isBackupInProgress = true;
                await CheckAndPerformBackups();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during backup check cycle: {ex.Message}");
            }
            finally
            {
                _isBackupInProgress = false;
            }
        }

        private async Task CheckAndPerformBackups()
        {
            var servers = LoadServers();
            if (servers == null || !servers.Any())
            {
                return;
            }

            foreach (var server in servers.Where(s => s.EnableAutoBackup))
            {
                try
                {
                    await CheckAndBackupServer(server);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error backing up server '{server.ProfileName}': {ex.Message}");
                }
            }
        }

        private async Task CheckAndBackupServer(ServerProfile server)
        {
            if (string.IsNullOrEmpty(server.ServerPath))
            {
                Logger.Log($"Server '{server.ProfileName}' has no ServerPath configured.");
                return;
            }

            string lastBackupFile = Path.Combine(server.ServerPath, "last_backup.txt");
            DateTime lastBackupTime = DateTime.MinValue;

            // Read last backup time
            if (File.Exists(lastBackupFile))
            {
                try
                {
                    string lastBackupText = File.ReadAllText(lastBackupFile);
                    if (DateTime.TryParse(lastBackupText, out DateTime parsedTime))
                    {
                        lastBackupTime = parsedTime;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error reading last backup time for '{server.ProfileName}': {ex.Message}");
                }
            }

            // Check if backup is due
            TimeSpan timeSinceLastBackup = DateTime.Now - lastBackupTime;
            if (timeSinceLastBackup.TotalMinutes >= server.BackupIntervalMinutes)
            {
                Logger.Log($"Backup due for server '{server.ProfileName}' (last backup: {lastBackupTime}, interval: {server.BackupIntervalMinutes} min)");
                await PerformBackup(server);
            }
        }

        public async Task PerformBackup(ServerProfile server)
        {
            if (string.IsNullOrEmpty(server.ServerPath))
            {
                Logger.Log($"Cannot backup server '{server.ProfileName}': ServerPath is not set.");
                return;
            }

            try
            {
                Logger.Log($"Starting backup for server '{server.ProfileName}'...");

                // Step 1: Send RCON saveworld command
                await SendSaveWorldCommand(server);

                // Step 2: Wait for save to flush to disk
                await Task.Delay(8000); // 8 seconds

                // Step 3: Copy SavedArks folder to backup location
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupRoot = Path.Combine(server.ServerPath, "Backups");
                string backupFolder = Path.Combine(backupRoot, timestamp);

                Directory.CreateDirectory(backupFolder);

                string sourceFolder = Path.Combine(server.ServerPath, "ShooterGame", "Saved", "SavedArks");
                if (!Directory.Exists(sourceFolder))
                {
                    Logger.Log($"Source folder not found for server '{server.ProfileName}': {sourceFolder}");
                    return;
                }

                string destinationFolder = Path.Combine(backupFolder, "SavedArks");
                CopyDirectory(sourceFolder, destinationFolder);

                Logger.Log($"Backup created for server '{server.ProfileName}' at: {backupFolder}");

                // Step 4: Prune old backups
                PruneOldBackups(backupRoot, server.MaxBackupCount);

                // Step 5: Update last backup time
                string lastBackupFile = Path.Combine(server.ServerPath, "last_backup.txt");
                File.WriteAllText(lastBackupFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                Logger.Log($"Backup completed successfully for server '{server.ProfileName}'.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error performing backup for server '{server.ProfileName}': {ex.Message}");
            }
        }

        private async Task SendSaveWorldCommand(ServerProfile server)
        {
            try
            {
                if (server.RCONPort <= 0 || string.IsNullOrEmpty(server.AdminPassword))
                {
                    Logger.Log($"RCON not configured for server '{server.ProfileName}', skipping saveworld command.");
                    return;
                }

                var rconService = new ArkRCONService("127.0.0.1", (ushort)server.RCONPort, server.AdminPassword, server.ServerPath);
                await rconService.ConnectAsync();
                await rconService.SaveWorldAsync();
                Logger.Log($"RCON saveworld command sent successfully for server '{server.ProfileName}'.");
                rconService.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"Warning: Could not send RCON saveworld for '{server.ProfileName}': {ex.Message}. Backup will continue anyway.");
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        private void PruneOldBackups(string backupRoot, int maxBackupCount)
        {
            try
            {
                if (!Directory.Exists(backupRoot))
                {
                    return;
                }

                var backupDirs = Directory.GetDirectories(backupRoot)
                    .Select(d => new DirectoryInfo(d))
                    .OrderByDescending(d => d.CreationTime)
                    .ToList();

                int backupsToDelete = backupDirs.Count - maxBackupCount;
                if (backupsToDelete > 0)
                {
                    Logger.Log($"Pruning {backupsToDelete} old backup(s) to maintain max count of {maxBackupCount}.");

                    foreach (var dirToDelete in backupDirs.Skip(maxBackupCount))
                    {
                        try
                        {
                            Directory.Delete(dirToDelete.FullName, true);
                            Logger.Log($"Deleted old backup: {dirToDelete.Name}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Error deleting backup directory '{dirToDelete.Name}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error pruning old backups: {ex.Message}");
            }
        }

        private List<ServerProfile> LoadServers()
        {
            try
            {
                if (!File.Exists(_serversJsonPath))
                {
                    return new List<ServerProfile>();
                }

                string serversJson = File.ReadAllText(_serversJsonPath);
                return JsonConvert.DeserializeObject<List<ServerProfile>>(serversJson) ?? new List<ServerProfile>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading servers for backup: {ex.Message}");
                return new List<ServerProfile>();
            }
        }

        public DateTime? GetLastBackupTime(ServerProfile server)
        {
            if (string.IsNullOrEmpty(server.ServerPath))
            {
                return null;
            }

            string lastBackupFile = Path.Combine(server.ServerPath, "last_backup.txt");
            if (File.Exists(lastBackupFile))
            {
                try
                {
                    string lastBackupText = File.ReadAllText(lastBackupFile);
                    if (DateTime.TryParse(lastBackupText, out DateTime parsedTime))
                    {
                        return parsedTime;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        public void Stop()
        {
            _backupCheckTimer?.Stop();
            _backupCheckTimer?.Dispose();
            Logger.Log("BackupService stopped.");
        }
    }
}
