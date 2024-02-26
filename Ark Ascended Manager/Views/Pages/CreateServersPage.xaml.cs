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
using Ark_Ascended_Manager.Services;


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
            // Check if ProfileName is not set
            if (string.IsNullOrWhiteSpace(ViewModel.ProfileName))
            {
                MessageBox.Show("Please enter a Profile Name first. Please ensure the Profile Name is the folder the server is in. The names must MACTCH EXACTLY! If not the server configs will not load.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Exit the method if no ProfileName is set
            }

            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select the folder for the server path";

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.SelectedPath;
                string desiredPath;

                // Get the name of the selected folder
                string selectedFolderName = new DirectoryInfo(folderPath).Name;

                // Check if the selected folder's name is already the ProfileName
                if (selectedFolderName.Equals(ViewModel.ProfileName, StringComparison.OrdinalIgnoreCase))
                {
                    desiredPath = folderPath;
                }
                else
                {
                    desiredPath = Path.Combine(folderPath, ViewModel.ProfileName);
                }

                ViewModel.ServerPath = desiredPath;
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
        {"PerLevelStatsMultiplier_DinoTamed[0]", ".2"},
        {"PerLevelStatsMultiplier_DinoTamed[1]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[2]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[3]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[4]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed[7]", "1 "},
        {"PerLevelStatsMultiplier_DinoTamed[8]", ".17 "},
        {"PerLevelStatsMultiplier_DinoTamed[9]", "1 "},
        {"PerLevelStatsMultiplier_DinoTamed_Add[0]", ".14"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[1]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[2]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[3]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[4]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[5]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[6]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[7]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[8]", ".14"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[9]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Add[10]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[0]", ".44"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[1]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[2]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[3]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[4]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[5]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[6]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[7]", "1"},
        {"PerLevelStatsMultiplier_DinoTamed_Affinity[8]", ".44"},
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
        {"GlobalSpoilingTimeMultiplier", "1"},
         {"HairGrowthSpeedMultiplier", "1"},
         {"MaxAlliancesPerTribe", "0"},
        {"GlobalItemDecompositionTimeMultiplier", "1"},
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
        { "BabyImprintAmountMultiplier", "1"},
        {"CustomRecipeEffectivenessMultiplier", "1"},
        {"CustomRecipeSkillMultiplier", "1"},
        {"AutoPvEStartTimeSeconds", "0"},
        {"bAllowUnclaimDinos", "True"},
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
        {"bDisableDinoBreeding", "False"},
        {"bDisableLootCrates", "False"},
        {"bDisableDinoTaming", "False"},
        {"bDisableDinoRiding", "False"},
        {"bDisableFriendlyFire", "False"},
        {"bFlyerPlatformAllowUnalignedDinoBasing", "False"},
        {"bIgnoreStructuresPreventionVolumes", "False"},
        {"bDisableLootCates", "False"},
        {"bAllowCustomRecipes", "True"},
        {"bPassiveDefensesDamageRiderlessDinos", "False"},
        {"bPvEAllowTrbeWar", "True"},
        {"bPvEAllowTribeWarCancel", "False"},
        {"MaxDifficulty", "False"},
        {"bUseSingleplayerSettings", "False"},
        {"bUseCorpseLocator", "True"},
        {"bShowCreatieMode", "False"},
        {"bHardLimitTurretsInRange", "True"},
          {"LimitTurretsNum", "100"},
          {"LimitTurretsRange", "10000"},
        {"bLimitTurretsInRange", "True"},
        {"bDisableStructurePlacementCollision", "False"},
        {"bAllowPlatformSaddleMultiFloors", "False"},
        {"bAllowUnlimitedRespecs", "True"},
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
        {"FishingLootQualityMultiplier", "1"},
        {"CraftingSkillBonusMultiplier", "1"},
        {"bAllowSpeedLeveling", "False"},
        {"bPvEDisableFriendlyFire", "False" },
        {"bAllowFlyerSpeedLeveling", "False"},
        {"bUseDinoLevelUpAnimations", "False"},
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
                            {"HarvestHealthMultiplier", "1" },
                            {"AllowThirdPersonPlayer", "True" },
                            {"AllowCaveBuildingPvE", "False" },
                            {"AlwaysNotifyPlayerJoined", "False" },
                            {"AlwaysNotifyPlayerLeft", "False" },
                            {"AllowFlyerCarryPvE", "False" },
                            {"AllowCaveBuildingPvP", "True" },
                            {"DisableStructureDecayPvE", "False" },
                            {"GlobalVoiceChat", "False" },
                            {"MaxStructuresInRange", "6700" },
                            {"NoTributeDownloads", "True" },
                            {"AllowCryoFridgeOnSaddle", "True" },
                            {"DisableCryopodFridgeRequirement", "True" },
                             {"DisableCryopodEnemyCheck", "True" },
                            {"PreventDownloadItems", "True" },
                            {"PreventDownloadDinos", "True" },
                            {"ProximityChat", "False" },
                             {"DestroyTamesOverTheSoftTameLimit", "True" },
                             {"MaxTamedDinos_SoftTameLimit", "5000" },
                             {"MaxTamedDinos_SoftTameLimit_CountdownForDeletionDuration", "604800" },
                            {"ResourceNoReplenishRadiusStructures", "1" },
                            {"ServerAdminPassword", "pleasechangethis" },
                            {"ServerCrosshair", "False" },
                            {"ServerForceNoHud", "False" },
                            {"ServerHardcore", "False" },
                            {"ServerPassword", "" },
                            {"ServerPvE", "False" },
                            {"ShowMapPlayerLocation", "True" },
                            {"TamedDinoDamageMultiplier", "1" },
                            {"TamedDinoResistanceMultiplier", "1" },
                             {"DinoDamageMultiplier", "1" },
                            {"DinoResistanceMultiplier", "1" },
                            {"TamingSpeedMultiplier", "1" },
                            {"XPMultiplier", "1" },
                            {"EnablePVPGamma", "False" },
                            {"EnablePVEGamma", "False" },
                            {"SpectatorPassword", "Password" },
                            {"DifficultyOffset", "1" },
                            {"NightTimeSpeedScale", "1" },
                            {"FastDecayUnsnappedCoreStructures", "False" },
                            {"PvEStructureDecayDestructionPeriod", "1" },
                            {"Banlist", "http" },
                            {"PvPStructureDecay", "0" },
                            {"DisableDinoDecayPvE", "False" },
                            {"PvEDinoDecayPeriodMultiplier", "1" },
                            {"AdminLogging", "False" },
                            {"MaxTamedDinos", "5000" },
                            {"MaxNumbersofPlayersInTribe", "60" },
                            {"BattleNumOfTribestoStartGame", "2" },
                            {"TimeToCollapseROD", "100" },
                            {"BattleAutoStartGameInterval", "100" },
                            {"BattleSuddenDeathInterval", "300" },
                            {"KickIdlePlayersPeriod", "3600" },
                            {"PerPlatformMaxStructuresMultiplier", "1" },
                            {"ForceAllStructureLocking", "False" },
                            {"AutoDestroyOldStructuresMultiplier", "0" },
                            {"UseVSync", "False" },
                            {"MaxPlatformSaddleStructureLimit", "20" },
                             {"PassiveDefensesDamageRiderlessDinos", "False" },
                             { "PreventSpawnAnimations", "False" },
                             { "PreventTribeAlliances", "False" },
                            {"RCONPort", "27020" },
                            {"AutoSavePeriodMinutes", "15" },
                            {"RCONServerGameLogBuffer", "600" },
                            {"OverrideStructurePlatformPrevention", "False" },
                            {"PreventOfflinePvPInterval", "0" },
                             {"ResourcesRespawnPeriodMultiplier", "1" },
                            {"bPvPDinoDecay", "False" },
                            {"bPvPStructureDecay", "False" },
                            {"DisableImprintDinoBuff", "False" },
                            {"AllowAnyoneBabyImprintCuddle", "False" },
                            {"EnableExtraStructurePreventionVolumes", "False" },
                            {"ShowFloatingDamageText", "False" },
                            {"DestroyUnconnectedWaterPipes", "False" },
                            {"DisablePvEGamma", "False" },
                            {"OverrideOfficialDifficulty", "0" },
                            {"TheMaxStructuresInRange", "10500" },
                            {"MinimumDinoReuploadInterval", "False" },
                            {"PvEAllowStructuresAtSupplyDrops", "False" },
                            {"NPCNetworkStasisRangeScalePlayerCountStart", "0" },
                            {"NPCNetworkStasisRangeScalePlayerCountEnd", "0" },
                            {"NPCNetworkStasisRangeScalePercentEnd", "0.55000001" },
                            {"MaxPersonalTamedDinos", "0" },
                            {"AutoDestroyDecayedDinos", "False" },
                            {"ClampItemSpoilingTimes", "False" },
                            {"UseOptimizedHarvestingHealth", "True" },
                            {"AllowCrateSpawnsOnTopOfStructures", "False" },
                            {"ForceFlyerExplosives", "False" },
                            {"PreventOfflinePvP", "False" },
                            {"AllowFlyingStaminaRecovery", "False" },
                            {"OxygenSwimSpeedStatMultiplier", "1" },
                            
                            {"ServerAutoForceRespawnWildDinosInterval", "3600" },
                            {"DisableWeatherFog", "False" },
                            {"DayCycleSpeedScale", "1" },
                            {"DayTimeSpeedScale", "1" },
                            {"RandomSupplyCratePoints", "False" },
                            {"PreventDiseases", "False" },
                            {"PreventMateBoost", "False" },
                             {"DontAlwaysNotifyPlayerJoined", "False" },
                            {"ClampResourceHarvestDamage", "False" },
                            {"CrossARKAllowForeignDinoDownloads", "False" },
                            {"PersonalTamedDinosSaddleStructureCost", "19" },
                            {"StructurePreventResourceRadiusMultiplier", "1" },
                            {"StructureResistanceMultiplier", "1" },
                            {"TribeNameChangeCooldown", "15" },
                            {"PlayerCharacterHealthRecoveryMultiplier", "1" },
                            {"PlayerCharacterStaminaDrainMultiplier", "1" },
                            {"PlayerCharacterWaterDrainMultiplier", "1" },
                            {"PlayerDamageMultiplier", "1" },
                            {"PlayerResistanceMultiplier", "1" },
                            {"PlatformSaddleBuildAreaBoundsMultiplier", "1" },
                            {"AlwaysAllowStructurePickup", "False" },
                            {"StructurePickupTimeAfterPlacement", "30" },
                            {"StructurePickupHoldDuration", "1" },
                            {"PlayerCharacterFoodDrainMultiplier", "1" },
                            {"AllowHideDamageSourceFromLogs", "True" },
                            {"RaidDinoCharacterFoodDrainMultiplier", "1" },
                            {"DinoCharacterHealthRecoveryMultiplier", "1" },
                            {"DinoCharacterStaminaDrainMultiplier", "1" },
                            {"ItemStackSizeMultiplier", "1" },
                            {"AllowMultipleAttachedC4", "False" },
                            {"AllowRaidDinoFeeding", "False" },
                            {"AllowHitMarkers", "True" }
            // Add more key-value pairs under ServerSettings
        }},
        { "SessionSettings", new Dictionary<string, string> {
            { "SessionName",  ViewModel.ServerName }
            // Add more key-value pairs under SessionSettings
        }},
                     {
        "MessageOfTheDay", new Dictionary<string, string>
        {
            {"Message", "Welcome! This needs to be updated :)" }

        }
    },
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
                UpdateChangeNumberToLatest(newServerConfig);
                SaveServerConfig(newServerConfig);
                CreateServerLaunchBatchFile(newServerConfig);
                ResetViewModel();
                

            }
            catch (Exception ex)
            {
                SystemMessageBox.Show($"An error occurred: {ex.Message}", "Error", SystemMessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateChangeNumberToLatest(ServerConfig serverConfig)
        {
            // Assume LoadSanitizedData method returns the latest sanitized data for the given AppId
            var latestSanitizedData = LoadSanitizedData(serverConfig.AppId);
            if (latestSanitizedData != null)
            {
                serverConfig.ChangeNumber = latestSanitizedData.ChangeNumber;
            }
            else
            {
                Logger.Log("Issues retrieving sanitized steam db data");
            }
        }
        private SanitizedSteamData LoadSanitizedData(string appId)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sanitizedDataFilePath = Path.Combine(appDataFolder, "Ark Ascended Manager", appId, $"sanitizedsteamdata_{appId}.json");

            if (!File.Exists(sanitizedDataFilePath))
            {
                // Log or handle the error as needed if the file doesn't exist
                Logger.Log($"Sanitized data file not found for AppId: {appId}");
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(sanitizedDataFilePath);
                var sanitizedData = System.Text.Json.JsonSerializer.Deserialize<SanitizedSteamData>(jsonContent);
                return sanitizedData;
            }
            catch (System.Text.Json.JsonException ex)
            {
                // Log or handle the error as needed if deserialization fails
                Logger.Log($"Error deserializing the sanitized data file for AppId: {appId}. Error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Log or handle any other exceptions as needed
                Logger.Log($"An unexpected error occurred while loading sanitized data for AppId: {appId}. Error: {ex.Message}");
                return null;
            }
        }

        public class SanitizedSteamData
        {
            public int ChangeNumber { get; set; }
            // Include other properties that the sanitized JSON may contain
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

start {serverExecutablePath} {config.MapName}?listen?""SessionName=%ServerName%?""RCONEnabled=True?""ServerPassword=%ServerPassword%?""Port=%Port%?RCONPort=%RconPort%?""ServerAdminPassword=%AdminPassword%"" {additionalSettings}
".Trim();

            // Define the path for the batch file within the AAM directory
            string batchFilePath = Path.Combine(config.ServerPath, "LaunchServer.bat");

            // Write the content to the batch file
            File.WriteAllText(batchFilePath, batchFileContent);


          
        }



        public class ServerConfig
        {
            public string ChangeNumberStatus { get; set; }
            public bool IsMapNameOverridden { get; set; }
            public string ProfileName { get; set; }
            public int? Pid { get; set; }
            public string ServerStatus { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }
            public bool IsRunning { get; set; }
            public int ChangeNumber { get; set; }
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