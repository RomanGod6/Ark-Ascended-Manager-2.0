using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using Ookii.Dialogs.Wpf;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using WpfMessageBox = Wpf.Ui.Controls.MessageBox;
using SystemMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = Wpf.Ui.Controls.MessageBoxButton;
using SystemMessageBoxButton = System.Windows.MessageBoxButton;
using System.Diagnostics;
using System.Text.Json;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using Newtonsoft.Json;


namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class CreateServersPage : INavigableView<CreateServersPageViewModel> // Assuming DataViewModel is still appropriate
    {
        private readonly INavigationService _navigationService;
        public CreateServersPageViewModel ViewModel { get; }

        public CreateServersPage(CreateServersPageViewModel viewModel)
        {
            InitializeComponent();
            
            ViewModel = viewModel;
            DataContext = ViewModel;
        }
        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select the folder for the server path";

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.SelectedPath;
                // Use the ProfileName from the ViewModel instead of "ServerProfile"
                string desiredPath = Path.Combine(folderPath, ViewModel.ProfileName);
                ViewModel.ServerPath = desiredPath; // Set the property on ViewModel
            }
        }

        private void SaveServerConfig(ServerConfig config)
        {
            // Get the folder path for the current user's AppData directory
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Combine the AppData path with your application's specific folder
            string applicationFolderPath = Path.Combine(appDataFolderPath, "Ark Ascended Manager");

            // Ensure the directory exists
            Directory.CreateDirectory(applicationFolderPath);

            // Define the file path for servers.json within the application's folder
            string filePath = Path.Combine(applicationFolderPath, "servers.json");

            List<ServerConfig> servers;

            if (File.Exists(filePath))
            {
                // Read existing list from file
                string json = File.ReadAllText(filePath);
                servers = JsonConvert.DeserializeObject<List<ServerConfig>>(json) ?? new List<ServerConfig>();
            }
            else
            {
                servers = new List<ServerConfig>();
            }

        
            // Add the new server config
            servers.Add(config);

            // Write updated list to file
            string updatedJson = JsonConvert.SerializeObject(servers, Formatting.Indented);

            File.WriteAllText(filePath, updatedJson);
            System.Windows.MessageBox.Show($"Server config saved to: {filePath}", "Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private string FindSteamCmdPath()
        {
            // Define the JSON file path
            string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "SteamCmdPath.json");

            // Try to read the path from the JSON file
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                dynamic pathData = JsonConvert.DeserializeObject(json);
                string savedPath = pathData?.SteamCmdPath;
                if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                {
                    return savedPath;
                }
            }

            // Check in the default location
            string defaultPath = @"C:\SteamCMD\steamcmd.exe";
            if (File.Exists(defaultPath))
            {
                // Optionally, save the default path to the JSON file
                SaveSteamCmdPath(defaultPath, jsonFilePath);
                return defaultPath;
            }

            // If not found, prompt the user
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Locate steamcmd.exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Save the selected path to the JSON file
                SaveSteamCmdPath(openFileDialog.FileName, jsonFilePath);
                return openFileDialog.FileName;
            }

            return null; // or handle this case appropriately
        }

        private void SaveSteamCmdPath(string path, string jsonFilePath)
        {
            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(jsonFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var pathData = new { SteamCmdPath = path };
            string json = JsonConvert.SerializeObject(pathData, Formatting.Indented);

            // Now it's safe to write the file as the directory exists
            File.WriteAllText(jsonFilePath, json);
        }

        private void RunSteamCMD(string scriptPath)
        {
            string steamCmdPath = FindSteamCmdPath();
            if (string.IsNullOrEmpty(steamCmdPath))
            {
                SystemMessageBox.Show("steamcmd.exe not found. Please locate the file.", "Error", SystemMessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = steamCmdPath,
                Arguments = "+runscript \"" + scriptPath + "\"",
                UseShellExecute = true, // Change to true to show the SteamCMD window
                RedirectStandardOutput = false, // No need to redirect
                CreateNoWindow = false // SteamCMD should create its own window
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                process.WaitForExit(); // Wait for the process to complete
            }
        }
        private void ResetViewModel()
        {
            // Reset all properties in ViewModel to their default values
            ViewModel.ProfileName = string.Empty;
            ViewModel.ServerPath = string.Empty;
            ViewModel.SelectedOption = null; // Or your default selection if applicable
                                             // For AppId, you may not need to reset it as it is a lookup based on the selected map.
            ViewModel.ServerName = string.Empty;
            ViewModel.ListenPort = 27015; // Default port, or empty string if you want it to be filled each time.
            ViewModel.RCONPort = 27020; // Default RCON port, or empty string if you want it to be filled each time.
            ViewModel.Mods = string.Empty; // Assuming this is a comma-separated string in your ViewModel.
            ViewModel.AdminPassword = string.Empty;
            ViewModel.ServerPassword = string.Empty;
            ViewModel.UseBattlEye = false;
            ViewModel.ForceRespawnDinos = false;
            ViewModel.PreventSpawnAnimation = false;
            //... reset other properties as needed
        }

        




        private void CreateServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                string baseServerPath = ViewModel.ServerPath;
                string profileName = ViewModel.ProfileName;
                string selectedMap = ViewModel.SelectedOption; // Assuming this is the selected map name

                // Construct the final server path
                string aamDirectory = Path.Combine(baseServerPath);
                if (!Directory.Exists(aamDirectory))
                {
                    Directory.CreateDirectory(aamDirectory);
                }
                string finalPath = ViewModel.ServerPath;
                Directory.CreateDirectory(finalPath);

                // Get Steam App ID
                string appId = ViewModel.MapToAppId.ContainsKey(selectedMap) ? ViewModel.MapToAppId[selectedMap] : "default_app_id";

                // Create SteamCMD script
                string scriptPath = Path.Combine(Path.GetTempPath(), "steamcmd_script.txt");
                File.WriteAllLines(scriptPath, new string[]
                {
                $"force_install_dir \"{finalPath}\"",
                "login anonymous",
                $"app_update {appId} validate",
                "quit"
                });
                RunSteamCMD(scriptPath);



                Dictionary<string, string> gameIniDefaults = new Dictionary<string, string>
    {
        {"BabyImprintingStatScaleMultiplier", "1"},
        {"BabyCuddleIntervalMultiplier", "1"},
        {"BabyCuddleGracePeriodMultiplier", "1"},
        {"BabyCuddleLoseImprintQualitySpeedMultiplier", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[0]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[1]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[2]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[3]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[4]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[7]", "1 "},
        {"PerLevelStatsMultiplier_DinoTamed[8]", "1 "},
        {"PerLevelStatsMultiplier_DinoTamed[9]", "1 "},
        {"PerLevelStatsMultiplier_DinoTamed_Add[0]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[1]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[2]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[3]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[4]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[5]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[6]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[7]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[8]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[9]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[10]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[0]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[1]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[2]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[3]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[4]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[5]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[6]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[7]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[8]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[9]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[10]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[0]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[1]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[2]", "0"},
        {"PerLevelStatsMultiplier_DinoWild[3]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[4]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[5]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[6]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[7]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[8]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[9]", "1"},
        {"PerLevelStatsMultiplier_DinoWild[10]", "1"},
        {"PerLevelStatsMultiplier_Player[0]", "1"},
        {"PerLevelStatsMultiplier_Player[1]", "1"},
        {"PerLevelStatsMultiplier_Player[2]", "1"},
        {"PerLevelStatsMultiplier_Player[3]", "1"},
        {"PerLevelStatsMultiplier_Player[4]", "1"},
        {"PerLevelStatsMultiplier_Player[5]", "1"},
        {"PerLevelStatsMultiplier_Player[6]", "1"},
        {"PerLevelStatsMultiplier_Player[7]", "1"},
        {"PerLevelStatsMultiplier_Player[8]", "1"},
        {"PerLevelStatsMultiplier_Player[9]", "1"},
        {"PerLevelStatsMultiplier_Player[10]", "1"},
        {"GlobalSpoilingTimeMultiplier", "0"},
        {"GlobalItemDecompositionTimeMultiplier", "0"},
        {"GlobalCorpseDecompositionTimeMultiplier", "1"},
        {"PvPZoneStructureDamageMultiplier", "6"},
        {"StructureDamageRepairCooldown", "1"},
        {"IncreasePvPRespawnIntervalCheckPeriod", "3"},
        {"IncreasePvPRespawnIntervalMultiplier", "2"},
        {"IncreasePvPRespawnIntervalBaseAmount", "5"},
        {"ResourceNoReplenishRadiusPlayers", "1"},
        {"CropGrowthSpeedMultiplier", "1"},
        {"LayEggIntervalMultiplier", "1"},
        {"PoopIntervalMultiplie", "1"},
        {"CropDecaySpeedMultiplier", "1"},
        {"MatingIntervalMultiplier", "1"},
        {"EggHatchSpeedMultiplier", "1"},
        {"BabyMatureSpeedMultiplier", "1"},
        {"BabyFoodConsumptionSpeedMultiplier", "1"},
        {"DinoTurretDamageMultiplier", "1"},
        {"DinoHarvestingDamageMultiplier", "1"},
        { "PlayerHarvestingDamageMultiplier", "1"},
        {"CustomRecipeEffectivenessMultiplier", "1"},
        {"CustomRecipeSkillMultiplier", "1"},
        {"AutoPvEStartTimeSeconds", "0"},
        {"AutoPvEStopTimeSecond", "0"},
        {"KillXPMuliplier", "1"},
        {"HarvestXPMultipier", "1"},
        {"CraftXPMultplier", "1"},
        {"GenericXPMultipier", "1"},
        {"SpecialXPMultipier", "1"},
        {"FuelConsumptionIntervalMultiplier", "0.25"},
        {"PhotoModeRangeLmit", "3000"},
        {"bDisablePhooMode", "False"},
        {"bIncreasePvPRespawnInterval", "True"},
        {"bAuoPvETimer", "False"},
        {"bAutoPvEUseSystemTie", "False"},
        {"bDisableFriendlyFire", "False"},
        {"bFlyerPlatformAllowUnalignedDinoBasing", "False"},
        {"bDisableLootCates", "False"},
        {"bAllowCustomRecipes", "True"},
        {"bPassiveDefensesDamageRiderlessDinos", "True"},
        {"bPvEAllowTrbeWar", "True"},
        {"bPvEAllowTribeWarCancel", "True"},
        {"MaxDifficulty", "False"},
        {"bUseSingleplayerSettings", "True"},
        {"bUseCorpseLocator", "True"},
        {"bShowCreatieMode", "False"},
        {"bHardLimitTurretsInRange", "True"},
        {"bDisableStructurePlacementCollision", "True"},
        {"bAllowPlatformSaddleMultiFloors", "False"},
        {"bAllowUnlimitedRespec", "True"},
        {"bDisableDinoTming", "False"},
        {"OverrideMaxExperiencePointsDino", "0"},
        {"MaxNumberOfPlayersInTribe", "0"},
        {"ExplorerNoteXPMultiplier", "0.999989986"},
        {"BossKillXPMultipler", "0.999989986"},
        {"AlphaKillXPMultiplir", "0.999989986"},
        {"WildKillXPMultipler", "0.999989986"},
        {"CaveKillXPMultipler", "0.999989986"},
        {"TamedKillXPMultiplir", "0.999989986"},
        {"UnclaimedKillXPMultiplier", "0.999989986"},
        {"SupplyCrateLootQualityMultiplier", "1"},
        {"FishingLootQualityMultiplierT", "1"},
        {"CraftingSkillBonusMultiplier", "1"},
        {"bAllowSpeedLeveling", "False"},
        {"bAllowFlyerSpeedLeveling", "False"},
        // Add more key-value pairs as needed
    };

                // Define the configuration path
                string configPath = Path.Combine(finalPath, "ShooterGame", "Saved", "Config", "WindowsServer");
                Directory.CreateDirectory(configPath);

                // Construct Game.ini content
                string gameIniContent = "[/Script/ShooterGame.ShooterGameMode]\n";
                foreach (var pair in gameIniDefaults)
                {
                    gameIniContent += $"{pair.Key}={pair.Value}\n";
                }
                File.WriteAllText(Path.Combine(configPath, "Game.ini"), gameIniContent);

                // Define default values for GameUserSettings.ini
                Dictionary<string, Dictionary<string, string>> gameUserSettingsDefaults = new Dictionary<string, Dictionary<string, string>>
    {
        { "ServerSettings", new Dictionary<string, string> {
                            {"HarvestAmountMultiplier", "1" },
                            {"AllowThirdPersonPlayer", "False" },
                            {"AllowCaveBuildingPvE", "False" },
                            {"AlwaysNotifyPlayerJoined", "False" },
                            {"AlwaysNotifyPlayerLeft", "False" },
                            {"AllowFlyerCarryPvE", "False" },
                            {"DisableStructureDecayPvE", "False" },
                            {"GlobalVoiceChat", "False" },
                            {"MaxStructuresInRange", "6700" },
                            {"NoTributeDownloads", "True" },
                            {"PreventDownloadSurvivors", "True" },
                            {"PreventDownloadItems", "True" },
                            {"PreventDownloadDinos", "True" },
                            {"ProximityChat", "False" },
                            {"ResourceNoReplenishRadiusStructures", "1" },
                            {"ServerAdminPassword", "pleasechangethis" },
                            {"ServerCrosshair", "False" },
                            {"ServerForceNoHud", "False" },
                            {"ServerHardcore", "False" },
                            {"ServerPassword", "" },
                            {"ServerPvE", "False" },
                            {"ShowMapPlayerLocation", "False" },
                            {"TamedDinoDamageMultiplier", "1" },
                            {"TamedDinoResistanceMultiplier", "1" },
                            {"TamingSpeedMultiplier", "1" },
                            {"XPMultiplier", "1" },
                            {"EnablePVPGamma", "False" },
                            {"EnablePVEGamma", "False" },
                            {"SpectatorPassword", "Password" },
                            {"DifficultyOffset", "1" },
                            {"PvEStructureDecayDestructionPeriod", "1" },
                            {"Banlist", "http" },
                            {"PvPStructureDecay", "0" },
                            {"DisableDinoDecayPvE", "False" },
                            {"PvEDinoDecayPeriodMultiplier", "1" },
                            {"AdminLogging", "False" },
                            {"MaxTamedDinos", "8000" },
                            {"MaxNumbersofPlayersInTribe", "60" },
                            {"BattleNumOfTribestoStartGame", "2" },
                            {"TimeToCollapseROD", "100" },
                            {"BattleAutoStartGameInterval", "100" },
                            {"BattleSuddenDeathInterval", "300" },
                            {"KickIdlePlayersPeriod", "1800" },
                            {"PerPlatformMaxStructuresMultiplier", "1" },
                            {"ForceAllStructureLocking", "False" },
                            {"AutoDestroyOldStructuresMultiplier", "0" },
                            {"UseVSync", "False" },
                            {"MaxPlatformSaddleStructureLimit", "20" },
                            {"PassiveDefensesDamageRiderlessDinos", "False" },
                            {"RCONPort", "27020" },
                            {"AutoSavePeriodMinutes", "20" },
                            {"RCONServerGameLogBuffer", "600" },
                            {"OverrideStructurePlatformPrevention", "False" },
                            {"PreventOfflinePvPInterval", "60" },
                            {"bPvPDinoDecay", "False" },
                            {"bPvPStructureDecay", "False" },
                            {"DisableImprintDinoBuff", "False" },
                            {"AllowAnyoneBabyImprintCuddle", "False" },
                            {"EnableExtraStructurePreventionVolumes", "False" },
                            {"ShowFloatingDamageText", "False" },
                            {"DestroyUnconnectedWaterPipes", "False" },
                            {"OverrideOfficialDifficulty", "5" },
                            {"TheMaxStructuresInRange", "10500" },
                            {"MinimumDinoReuploadInterval", "False" },
                            {"PvEAllowStructuresAtSupplyDrops", "False" },
                            {"NPCNetworkStasisRangeScalePlayerCountStart", "70" },
                            {"NPCNetworkStasisRangeScalePlayerCountEnd", "120" },
                            {"NPCNetworkStasisRangeScalePercentEnd", "0.50" },
                            {"MaxPersonalTamedDinos", "500" },
                            {"AutoDestroyDecayedDinos", "False" },
                            {"ClampItemSpoilingTimes", "False" },
                            {"UseOptimizedHarvestingHealth", "False" },
                            {"AllowCrateSpawnsOnTopOfStructures", "False" },
                            {"ForceFlyerExplosives", "False" },
                            {"PreventOfflinePvP", "False" },
                            {"AllowFlyingStaminaRecovery", "False" },
                            {"OxygenSwimSpeedStatMultiplier", "1" },
                            {"bPvEDisableFriendlyFire", "False" },
                            {"ServerAutoForceRespawnWildDinosInterval", "3600" },
                            {"DisableWeatherFog", "False" },
                            {"RandomSupplyCratePoints", "False" },
                            {"CrossARKAllowForeignDinoDownloads", "False" },
                            {"PersonalTamedDinosSaddleStructureCost", "19" },
                            {"StructurePreventResourceRadiusMultiplier", "1" },
                            {"TribeNameChangeCooldown", "15" },
                            {"PlatformSaddleBuildAreaBoundsMultiplier", "1" },
                            {"AlwaysAllowStructurePickup", "False" },
                            {"StructurePickupTimeAfterPlacement", "30" },
                            {"StructurePickupHoldDuration", "0" },
                            {"AllowHideDamageSourceFromLogs", "False" },
                            {"RaidDinoCharacterFoodDrainMultiplier", "1" },
                            {"ItemStackSizeMultiplier", "1" },
                            {"AllowHitMarkers", "False" }
            // Add more key-value pairs under ServerSettings
        }},
        { "SessionSettings", new Dictionary<string, string> {
            { "SessionName", "" }
            // Add more key-value pairs under SessionSettings
        }}
        // Add more sections as needed
    };

                // Construct GameUserSettings.ini content
                string gameUserSettingsContent = "";
                foreach (var section in gameUserSettingsDefaults)
                {
                    gameUserSettingsContent += $"[{section.Key}]\n";
                    foreach (var pair in section.Value)
                    {
                        gameUserSettingsContent += $"{pair.Key}={pair.Value}\n";
                    }
                }
                File.WriteAllText(Path.Combine(configPath, "GameUserSettings.ini"), gameUserSettingsContent);


                ServerConfig newServerConfig = new ServerConfig
                {
                    ProfileName = ViewModel.ProfileName,
                    ServerPath = ViewModel.ServerPath,
                    MapName = ViewModel.SelectedOption,
                    AppId = ViewModel.MapToAppId[ViewModel.SelectedOption],
                    ServerName = ViewModel.ServerName,
                    ListenPort = Convert.ToInt32(ViewModel.ListenPort), // Make sure to validate this conversion
                    RCONPort = Convert.ToInt32(ViewModel.RCONPort),

                    Mods = ViewModel.Mods?.Split(',').ToList(),         // Assuming Mods is a comma-separated string
                    AdminPassword = ViewModel.AdminPassword,
                    ServerPassword = ViewModel.ServerPassword,
                    UseBattlEye = ViewModel.UseBattlEye,
                    MaxPlayerCount = Convert.ToInt32(ViewModel.MaxPlayerCount),
                    ForceRespawnDinos = ViewModel.ForceRespawnDinos,
                    PreventSpawnAnimation = ViewModel.PreventSpawnAnimation
                    // ... Assign all other necessary properties from ViewModel ...
                };

                // Save the server config to the master list
                SaveServerConfig(newServerConfig);
                CreateServerLaunchBatchFile(newServerConfig);
                ResetViewModel();
                

            }
            catch (Exception ex)
            {
                SystemMessageBox.Show($"An error occurred: {ex.Message}", "Error", SystemMessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CreateServerLaunchBatchFile(ServerConfig config)
        {
            // Construct the full path to the ArkAscendedServer.exe
            string serverExecutablePath = Path.Combine(config.ServerPath, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe");

            // Prepare the mods string; only include it if Mods has any elements.
            string modSettings = config.Mods != null && config.Mods.Any() ? $"-mods={string.Join(",", config.Mods)}" : string.Empty;

            // Construct the additional settings, including the modSettings if it's not empty
            string additionalSettings = $"-WinLiveMaxPlayers=%MaxPlayers% -SecureSendArKPayload -ActiveEvent=none -NoTransferFromFiltering -UseBattlEye -forcerespawndinos -servergamelog -ServerRCONOutputTribeLogs -noundermeshkilling -nosteamclient -game -server -log -AutoDestroyStructures -UseBattlEye -NotifyAdminCommandsInChat {modSettings}".Trim();

            // Define the content of the batch file using the server configuration data
            var batchFileContent = $@"
set ServerName={config.ServerName}
set ServerPassword={config.ServerPassword}
set AdminPassword={config.AdminPassword}
set Port={config.ListenPort}
set RconPort={config.RCONPort}
set MaxPlayers={config.MaxPlayerCount}

start {serverExecutablePath} {config.MapName}?listen?SessionName=%ServerName%?RCONEnabled=True?ServerPassword=%ServerPassword%?Port=%Port%?RCONPort=%RconPort%?ServerAdminPassword=%AdminPassword% {additionalSettings}
".Trim();

            // Define the path for the batch file within the AAM directory
            string batchFilePath = Path.Combine(config.ServerPath, "LaunchServer.bat");

            // Write the content to the batch file
            File.WriteAllText(batchFilePath, batchFileContent);

            // Optionally, notify the user where the batch file was saved
            SystemMessageBox.Show($"Server launch batch file saved to: {batchFilePath}", "Batch File Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        public class ServerConfig
        {
            public string ProfileName { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }
            public bool IsRunning { get; set; }

            public string ServerName { get; set; }
            public int ListenPort { get; set; } // Ports are typically integers
            public int RCONPort { get; set; }   // Ports are typically integers
            public List<string> Mods { get; set; } // Assuming Mods can be a list
            public int MaxPlayerCount { get; set; }
            public string AdminPassword { get; set; }
            public string ServerPassword { get; set; }
            public bool UseBattlEye { get; set; } // Use bool for checkboxes
            public bool ForceRespawnDinos { get; set; } // Use bool for checkboxes
            public bool PreventSpawnAnimation { get; set; } // Use bool for checkboxes

            // ... other relevant details
        }

    }





}