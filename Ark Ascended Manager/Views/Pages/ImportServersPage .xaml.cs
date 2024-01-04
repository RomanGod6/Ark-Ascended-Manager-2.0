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

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ImportServersPage : INavigableView<ImportServersPageViewModel> // Assuming DataViewModel is still appropriate
    {
        private readonly INavigationService _navigationService;
        public ImportServersPageViewModel ViewModel { get; }

        public ImportServersPage(ImportServersPageViewModel viewModel)
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

                // Check if a ProfileName is set in the ViewModel
                if (string.IsNullOrEmpty(ViewModel.ProfileName))
                {
                    MessageBox.Show("Please enter a Profile Name before selecting a folder. Please note the Profile name MUST match the folder name for the import to be successful.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Exit the method if ProfileName is not set
                }

                // Check if the selected folder's name matches the ProfileName
                string selectedFolderName = new DirectoryInfo(folderPath).Name;
                if (selectedFolderName != ViewModel.ProfileName)
                {
                    MessageBox.Show($"The selected folder name must match the Profile Name '{ViewModel.ProfileName}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Exit the method if folder name doesn't match
                }

                // Use the folder path as is
                ViewModel.ServerPath = folderPath;
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
        private void SaveServerConfig(ServerConfig config)
        {
            try
            {
                // Get the AppData path for the current user
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string applicationFolderPath = Path.Combine(appDataPath, "Ark Ascended Manager");

                // Ensure the directory exists
                Directory.CreateDirectory(applicationFolderPath);

                // Define the servers.json file path
                string serversFilePath = Path.Combine(applicationFolderPath, "servers.json");

                // Initialize the servers list
                List<ServerConfig> servers = new List<ServerConfig>();

                // Read existing servers if the file exists
                if (File.Exists(serversFilePath))
                {
                    string existingJson = File.ReadAllText(serversFilePath);
                    servers = JsonSerializer.Deserialize<List<ServerConfig>>(existingJson) ?? new List<ServerConfig>();
                }

                // Add the new server configuration
                servers.Add(config);

                // Serialize the updated list of servers to JSON
                string updatedJson = JsonSerializer.Serialize(servers, new JsonSerializerOptions { WriteIndented = true });

                // Write the JSON to the servers.json file
                File.WriteAllText(serversFilePath, updatedJson);

                // Inform the user of success
                MessageBox.Show("Server configuration saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Inform the user of any errors
                MessageBox.Show($"Failed to save server configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ... [Other code]

        // This method creates a new ServerConfig object from the ViewModel and calls the save method.
        private void SaveImportedServer()
        {
            try
            {
                // Validation to ensure the ServerPath ends with the ProfileName
                if (!ViewModel.ServerPath.EndsWith(ViewModel.ProfileName))
                {
                    MessageBox.Show($"The Server Path must end with the Profile Name '{ViewModel.ProfileName}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Exit the method if validation fails
                }
                string gameIniPath = Path.Combine(ViewModel.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
                string gameUserSettingsIniPath = Path.Combine(ViewModel.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");


                // Create a new ServerConfig object from the ViewModel properties
                ServerConfig newServerConfig = new ServerConfig
                {
                    ProfileName = ViewModel.ProfileName,
                    ServerPath = ViewModel.ServerPath,
                    MapName = ViewModel.SelectedOption,
                    AppId = ViewModel.MapToAppId[ViewModel.SelectedOption],
                    ServerName = ViewModel.ServerName,
                    ListenPort = Convert.ToInt32(ViewModel.ListenPort),
                    RCONPort = Convert.ToInt32(ViewModel.RCONPort),
                    Mods = ViewModel.Mods?.Split(',').ToList(),
                    AdminPassword = ViewModel.AdminPassword,
                    ServerPassword = ViewModel.ServerPassword,
                    UseBattlEye = ViewModel.UseBattlEye,
                    MaxPlayerCount = Convert.ToInt32(ViewModel.MaxPlayerCount),
                    ForceRespawnDinos = ViewModel.ForceRespawnDinos,
                    PreventSpawnAnimation = ViewModel.PreventSpawnAnimation
                };
                UpdateGameIniSettings(gameIniPath);
                UpdateGameUserSettingsIniSettings(gameUserSettingsIniPath);

                // Call the SaveServerConfig method to save the new server
                SaveServerConfig(newServerConfig);

                // Reset the ViewModel properties to clear the form
                ResetViewModel();
            }
            catch (Exception ex)
            {
                // Handle exceptions here, such as showing an error message
                MessageBox.Show($"An error occurred while saving the server configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportServer_Click(object sender, RoutedEventArgs e)
        {
            SaveImportedServer();
        }
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
        private void UpdateGameIniSettings(string gameIniPath)
        {
            List<string> existingSettings = File.Exists(gameIniPath) ? File.ReadAllLines(gameIniPath).ToList() : new List<string>();
            bool headerExists = existingSettings.Any(line => line.Trim() == "[/Script/ShooterGame.ShooterGameMode]");

            if (!headerExists)
            {
                existingSettings.Add("[/Script/ShooterGame.ShooterGameMode]");
            }

            foreach (var setting in gameIniDefaults)
            {
                if (!existingSettings.Any(line => line.StartsWith(setting.Key + "=")))
                {
                    existingSettings.Add($"{setting.Key}={setting.Value}");
                }
            }

            File.WriteAllLines(gameIniPath, existingSettings);
        }
        private void UpdateGameUserSettingsIniSettings(string gameUserSettingsIniPath)
        {
            // Dictionary containing default values organized by sections
            Dictionary<string, Dictionary<string, string>> gameUserSettingsDefaults = new Dictionary<string, Dictionary<string, string>>
            {
                            {
        "ServerSettings", new Dictionary<string, string>
        {
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
            // ... more settings ...
        }
    },
    {
        "SessionSettings", new Dictionary<string, string>
        {
            {"SessionName", "" }

        }
    },
     {
        "MessageOfTheDay", new Dictionary<string, string>
        {
            {"Message", "" }

        }
    },
            };

            // Read existing settings or initialize an empty list if the file doesn't exist
            List<string> existingSettings = File.Exists(gameUserSettingsIniPath) ? File.ReadAllLines(gameUserSettingsIniPath).ToList() : new List<string>();

            // Track the current section while iterating through the file
            string currentSection = "";
            foreach (string line in existingSettings.ToList())
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Trim('[', ']');
                }
                else if (!string.IsNullOrWhiteSpace(line) && line.Contains("=") && gameUserSettingsDefaults.ContainsKey(currentSection))
                {
                    string key = line.Substring(0, line.IndexOf('=')).Trim();
                    if (gameUserSettingsDefaults[currentSection].ContainsKey(key))
                    {
                        // Update the existing setting if needed
                        gameUserSettingsDefaults[currentSection].Remove(key);
                    }
                }
            }

            // Add missing sections and settings
            foreach (var section in gameUserSettingsDefaults)
            {
                if (!existingSettings.Any(l => l.Trim().Equals($"[{section.Key}]")))
                {
                    existingSettings.Add($"[{section.Key}]");
                }

                foreach (var setting in section.Value)
                {
                    existingSettings.Add($"{setting.Key}={setting.Value}");
                }
            }

            // Write the updated settings back to the GameUserSettings.ini file
            File.WriteAllLines(gameUserSettingsIniPath, existingSettings);
        }






        public class ServerConfig
        {
            public string ProfileName { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }

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