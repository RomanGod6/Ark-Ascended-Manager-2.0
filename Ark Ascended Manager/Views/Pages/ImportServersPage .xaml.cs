using Ark_Ascended_Manager.ViewModels.Pages;
using Wpf.Ui.Controls;
using Ookii.Dialogs.Wpf;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ImportServersPage : INavigableView<ImportServersPageViewModel>
    {
        private readonly INavigationService _navigationService;
        public ImportServersPageViewModel ViewModel { get; }

        private string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "defaultServerConfig.json");

        public ImportServersPage(ImportServersPageViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
            EnsureJsonFileExists();
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select the folder for the server path"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.SelectedPath;

                if (string.IsNullOrEmpty(ViewModel.ProfileName))
                {
                    System.Windows.MessageBox.Show("Please enter a Profile Name before selecting a folder. Please note the Profile name MUST match the folder name for the import to be successful.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                string selectedFolderName = new DirectoryInfo(folderPath).Name;
                if (selectedFolderName != ViewModel.ProfileName)
                {
                    System.Windows.MessageBox.Show($"The selected folder name must match the Profile Name '{ViewModel.ProfileName}'.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                ViewModel.ServerPath = folderPath;

                // Find and parse the batch file
                var batchFile = Directory.GetFiles(folderPath, "*.bat", SearchOption.AllDirectories).FirstOrDefault();
                if (batchFile != null)
                {
                    ExtractSettingsFromBatchFile(batchFile);
                }
                else
                {
                    System.Windows.MessageBox.Show("No batch file found in the selected folder.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
        }

        private void ExtractSettingsFromBatchFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var settings = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"set (\w+)=(.*)");
                if (match.Success)
                {
                    settings[match.Groups[1].Value] = match.Groups[2].Value.Trim();
                }
            }

            if (settings.ContainsKey("ServerName"))
                ViewModel.ServerName = settings["ServerName"];
            if (settings.ContainsKey("Port") && int.TryParse(settings["Port"], out int port))
                ViewModel.ListenPort = port;
            if (settings.ContainsKey("RconPort") && int.TryParse(settings["RconPort"], out int rconPort))
                ViewModel.RCONPort = rconPort;
            if (settings.ContainsKey("MaxPlayers") && int.TryParse(settings["MaxPlayers"], out int maxPlayers))
                ViewModel.MaxPlayerCount = maxPlayers;
            if (settings.ContainsKey("mods"))
                ViewModel.Mods = settings["mods"];
            // Add other settings extraction as needed
        }


        private void SearchAndFillServerDetails(string folderPath)
        {
            var batFiles = Directory.GetFiles(folderPath, "*.bat", SearchOption.AllDirectories);
            if (batFiles.Length > 0)
            {
                var batFile = batFiles[0];
                ParseBatFile(batFile);
            }
        }

        private void ParseBatFile(string batFile)
        {
            var lines = File.ReadAllLines(batFile);
            foreach (var line in lines)
            {
                if (line.Contains("ServerName"))
                {
                    var value = GetValue(line);
                    ViewModel.ServerName = value;
                }
                else if (line.Contains("ListenPort"))
                {
                    var value = GetValue(line);
                    ViewModel.ListenPort = int.Parse(value);
                }
                else if (line.Contains("RCONPort"))
                {
                    var value = GetValue(line);
                    ViewModel.RCONPort = int.Parse(value);
                }
                else if (line.Contains("AdminPassword"))
                {
                    var value = GetValue(line);
                    ViewModel.AdminPassword = value;
                }
                else if (line.Contains("ServerPassword"))
                {
                    var value = GetValue(line);
                    ViewModel.ServerPassword = value;
                }
                else if (line.Contains("MapName"))
                {
                    var value = GetValue(line);
                    ViewModel.SelectedOption = value;
                }
                // Add more parsing rules as needed
            }
        }

        private string GetValue(string line)
        {
            var parts = line.Split('=');
            return parts.Length > 1 ? parts[1].Trim() : string.Empty;
        }

        private void ResetViewModel()
        {
            ViewModel.ProfileName = string.Empty;
            ViewModel.ServerPath = string.Empty;
            ViewModel.SelectedOption = null;
            ViewModel.ServerName = string.Empty;
            ViewModel.ListenPort = 27015;
            ViewModel.RCONPort = 27020;
            ViewModel.Mods = string.Empty;
            ViewModel.AdminPassword = string.Empty;
            ViewModel.ServerPassword = string.Empty;
            ViewModel.UseBattlEye = false;
            ViewModel.ForceRespawnDinos = false;
            ViewModel.PreventSpawnAnimation = false;
        }

        private void SaveServerConfig(ServerConfig config)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string applicationFolderPath = Path.Combine(appDataPath, "Ark Ascended Manager");
                Directory.CreateDirectory(applicationFolderPath);
                string serversFilePath = Path.Combine(applicationFolderPath, "servers.json");
                List<ServerConfig> servers = new List<ServerConfig>();

                if (File.Exists(serversFilePath))
                {
                    string existingJson = File.ReadAllText(serversFilePath);
                    servers = JsonSerializer.Deserialize<List<ServerConfig>>(existingJson) ?? new List<ServerConfig>();
                }

                servers.Add(config);
                string updatedJson = JsonSerializer.Serialize(servers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(serversFilePath, updatedJson);

                System.Windows.MessageBox.Show("Server configuration saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save server configuration: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SaveImportedServer()
        {
            try
            {
                if (!ViewModel.ServerPath.EndsWith(ViewModel.ProfileName))
                {
                    System.Windows.MessageBox.Show($"The Server Path must end with the Profile Name '{ViewModel.ProfileName}'.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                string gameIniPath = Path.Combine(ViewModel.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "Game.ini");
                string gameUserSettingsIniPath = Path.Combine(ViewModel.ServerPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");

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

                SaveServerConfig(newServerConfig);
                ResetViewModel();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred while saving the server configuration: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ImportServer_Click(object sender, RoutedEventArgs e)
        {
            SaveImportedServer();
        }

        private void EnsureJsonFileExists()
        {
            if (!File.Exists(jsonFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));
                string defaultJsonContent = @"
      {
  ""GameIniDefaults"": [
    {
      ""Header"": ""/Script/ShooterGame.ShooterGameMode"",
      ""Settings"": {
        ""BabyImprintingStatScaleMultiplier"": ""1"",
        ""BabyCuddleIntervalMultiplier"": ""1"",
        ""BabyCuddleGracePeriodMultiplier"": ""1"",
        ""BabyCuddleLoseImprintQualitySpeedMultiplier"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed[0]"": ""0.2"",
        ""PerLevelStatsMultiplier_DinoTamed[1]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed[2]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed[3]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed[4]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed[7]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed[8]"": ""0.17"",
        ""PerLevelStatsMultiplier_DinoTamed[9]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[0]"": ""0.14"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[1]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[2]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[3]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[4]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[5]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[6]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[7]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[8]"": ""0.14"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[9]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Add[10]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[0]"": ""0.44"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[1]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[2]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[3]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[4]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[5]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[6]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[7]"": ""1"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[8]"": ""0.44"",
        ""PerLevelStatsMultiplier_DinoTamed_Affinity[9]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[0]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[1]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[2]"": ""0"",
        ""PerLevelStatsMultiplier_DinoWild[3]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[4]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[5]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[6]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[7]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[8]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[9]"": ""1"",
        ""PerLevelStatsMultiplier_DinoWild[10]"": ""1"",
        ""PerLevelStatsMultiplier_Player[0]"": ""1"",
        ""PerLevelStatsMultiplier_Player[1]"": ""1"",
        ""PerLevelStatsMultiplier_Player[2]"": ""1"",
        ""PerLevelStatsMultiplier_Player[3]"": ""1"",
        ""PerLevelStatsMultiplier_Player[4]"": ""1"",
        ""PerLevelStatsMultiplier_Player[5]"": ""1"",
        ""PerLevelStatsMultiplier_Player[6]"": ""1"",
        ""PerLevelStatsMultiplier_Player[7]"": ""1"",
        ""PerLevelStatsMultiplier_Player[8]"": ""1"",
        ""PerLevelStatsMultiplier_Player[9]"": ""1"",
        ""PlayerBaseStatMultipliers[0]"": ""1"",
        ""PlayerBaseStatMultipliers[1]"": ""1"",
        ""PlayerBaseStatMultipliers[2]"": ""1"",
        ""PlayerBaseStatMultipliers[3]"": ""1"",
        ""PlayerBaseStatMultipliers[4]"": ""1"",
        ""PlayerBaseStatMultipliers[5]"": ""1"",
        ""PlayerBaseStatMultipliers[6]"": ""1"",
        ""PlayerBaseStatMultipliers[7]"": ""1"",
        ""GlobalSpoilingTimeMultiplier"": ""1"",
        ""GlobalItemDecompositionTimeMultiplier"": ""1"",
        ""GlobalCorpseDecompositionTimeMultiplier"": ""1"",
        ""PvPZoneStructureDamageMultiplier"": ""6"",
        ""StructureDamageRepairCooldown"": ""1"",
        ""IncreasePvPRespawnIntervalCheckPeriod"": ""3"",
        ""IncreasePvPRespawnIntervalMultiplier"": ""2"",
        ""IncreasePvPRespawnIntervalBaseAmount"": ""5"",
        ""ResourceNoReplenishRadiusPlayers"": ""1"",
        ""CropGrowthSpeedMultiplier"": ""1"",
        ""LayEggIntervalMultiplier"": ""1"",
        ""PoopIntervalMultiplier"": ""1"",
        ""CropDecaySpeedMultiplier"": ""1"",
        ""MatingIntervalMultiplier"": ""1"",
        ""EggHatchSpeedMultiplier"": ""1"",
        ""BabyMatureSpeedMultiplier"": ""1"",
        ""BabyFoodConsumptionSpeedMultiplier"": ""1"",
        ""DinoTurretDamageMultiplier"": ""1"",
        ""DinoHarvestingDamageMultiplier"": ""1"",
        ""PlayerHarvestingDamageMultiplier"": ""1"",
        ""BabyImprintAmountMultiplier"": ""1"",
        ""CustomRecipeEffectivenessMultiplier"": ""1"",
        ""CustomRecipeSkillMultiplier"": ""1"",
        ""AutoPvEStartTimeSeconds"": ""0"",
        ""AutoPvEStopTimeSeconds"": ""0"",
        ""KillXPMultiplier"": ""1"",
        ""HarvestXPMultiplier"": ""1"",
        ""CraftXPMultiplier"": ""1"",
        ""GenericXPMultiplier"": ""1"",
        ""SpecialXPMultiplier"": ""1"",
        ""FuelConsumptionIntervalMultiplier"": ""0.25"",
        ""PhotoModeRangeLimit"": ""3000"",
        ""DisablePhotoMode"": ""False"",
        ""DestroyTamesOverTheSoftTameLimit"": ""True"",
        ""AllowCryoFridgeOnSaddle"": ""True"",
        ""DisableCryopodFridgeRequirement"": ""True"",
        ""DisableCryopodEnemyCheck"": ""True"",
        ""IncreasePvPRespawnInterval"": ""True"",
        ""AutoPvETimer"": ""False"",
        ""AutoPvEUseSystemTime"": ""False"",
        ""BPvPDisableFriendlyFire"": ""False"",
        ""FlyerPlatformAllowUnalignedDinoBasing"": ""False"",
        ""DisableLootCrates"": ""False"",
        ""AllowCustomRecipes"": ""True"",
        ""PassiveDefensesDamageRiderlessDinos"": ""False"",
        ""PvEAllowTribeWar"": ""True"",
        ""PvEAllowTribeWarCancel"": ""False"",
        ""MaxDifficulty"": ""False"",
        ""UseSingleplayerSettings"": ""False"",
        ""UseCorpseLocator"": ""True"",
        ""ShowCreativeMode"": ""False"",
        ""PreventDiseases"": ""False"",
        ""NonPermanentDiseases"": ""False"",
        ""HardLimitTurretsInRange"": ""True"",
        ""bDisableStructurePlacementCollision"": ""False"",
        ""AllowPlatformSaddleMultiFloors"": ""False"",
        ""AllowUnlimitedRespec"": ""True"",
        ""DisableDinoTaming"": ""False"",
        ""DisableDinoBreeding"": ""False"",
        ""DisableDinoRiding"": ""False"",
        ""AllowUnclaimDinos"": ""True"",
        ""PreventMateBoost"": ""False"",
        ""ForceAllowCaveFlyers"": ""False"",
        ""OverrideMaxExperiencePointsDino"": ""0"",
        ""MaxNumberOfPlayersInTribe"": ""0"",
        ""ExplorerNoteXPMultiplier"": ""0.999989986"",
        ""BossKillXPMultiplier"": ""0.999989986"",
        ""AlphaKillXPMultiplier"": ""0.999989986"",
        ""WildKillXPMultiplier"": ""0.999989986"",
        ""CaveKillXPMultiplier"": ""0.999989986"",
        ""TamedKillXPMultiplier"": ""0.999989986"",
        ""UnclaimedKillXPMultiplier"": ""0.999989986"",
        ""SupplyCrateLootQualityMultiplier"": ""1"",
        ""FishingLootQualityMultiplier"": ""1"",
        ""CraftingSkillBonusMultiplier"": ""1"",
        ""AllowSpeedLeveling"": ""False"",
        ""AllowFlyerSpeedLeveling"": ""False""
      }
    }
  ],
  ""GameUserSettingsDefaults"": {
    ""ServerSettings"": {
      ""HarvestAmountMultiplier"": ""1"",
      ""HarvestHealthMultiplier"": ""1"",
      ""AllowThirdPersonPlayer"": ""True"",
      ""AllowCaveBuildingPvE"": ""False"",
      ""AlwaysNotifyPlayerJoined"": ""False"",
      ""AlwaysNotifyPlayerLeft"": ""False"",
      ""AllowFlyerCarryPvE"": ""False"",
      ""AllowCaveBuildingPvP"": ""True"",
      ""DisableStructureDecayPvE"": ""False"",
      ""GlobalVoiceChat"": ""False"",
      ""MaxStructuresInRange"": ""6700"",
      ""NoTributeDownloads"": ""True"",
      ""AllowCryoFridgeOnSaddle"": ""True"",
      ""DisableCryopodFridgeRequirement"": ""True"",
      ""DisableCryopodEnemyCheck"": ""True"",
      ""PreventDownloadItems"": ""True"",
      ""PreventDownloadDinos"": ""True"",
      ""ProximityChat"": ""False"",
      ""DestroyTamesOverTheSoftTameLimit"": ""True"",
      ""MaxTamedDinos_SoftTameLimit"": ""5000"",
      ""MaxTamedDinos_SoftTameLimit_CountdownForDeletionDuration"": ""604800"",
      ""ResourceNoReplenishRadiusStructures"": ""1"",
      ""ServerAdminPassword"": ""pleasechangethis"",
      ""ServerCrosshair"": ""False"",
      ""ServerForceNoHud"": ""False"",
      ""ServerHardcore"": ""False"",
      ""ServerPassword"": """",
      ""ServerPvE"": ""False"",
      ""ShowMapPlayerLocation"": ""True"",
      ""TamedDinoDamageMultiplier"": ""1"",
      ""TamedDinoResistanceMultiplier"": ""1"",
      ""DinoDamageMultiplier"": ""1"",
      ""DinoResistanceMultiplier"": ""1"",
      ""TamingSpeedMultiplier"": ""1"",
      ""XPMultiplier"": ""1"",
      ""EnablePVPGamma"": ""False"",
      ""EnablePVEGamma"": ""False"",
      ""SpectatorPassword"": ""Password"",
      ""DifficultyOffset"": ""1"",
      ""NightTimeSpeedScale"": ""1"",
      ""FastDecayUnsnappedCoreStructures"": ""False"",
      ""PvEStructureDecayDestructionPeriod"": ""1"",
      ""Banlist"": ""http"",
      ""PvPStructureDecay"": ""0"",
      ""DisableDinoDecayPvE"": ""False"",
      ""PvEDinoDecayPeriodMultiplier"": ""1"",
      ""AdminLogging"": ""False"",
      ""MaxTamedDinos"": ""5000"",
      ""MaxNumbersofPlayersInTribe"": ""60"",
      ""BattleNumOfTribestoStartGame"": ""2"",
      ""TimeToCollapseROD"": ""100"",
      ""BattleAutoStartGameInterval"": ""100"",
      ""BattleSuddenDeathInterval"": ""300"",
      ""KickIdlePlayersPeriod"": ""3600"",
      ""PerPlatformMaxStructuresMultiplier"": ""1"",
      ""ForceAllStructureLocking"": ""False"",
      ""AutoDestroyOldStructuresMultiplier"": ""0"",
      ""UseVSync"": ""False"",
      ""MaxPlatformSaddleStructureLimit"": ""20"",
      ""PassiveDefensesDamageRiderlessDinos"": ""False"",
      ""PreventSpawnAnimations"": ""False"",
      ""PreventTribeAlliances"": ""False"",
      ""RCONPort"": ""27020"",
      ""AutoSavePeriodMinutes"": ""15"",
      ""RCONServerGameLogBuffer"": ""600"",
      ""OverrideStructurePlatformPrevention"": ""False"",
      ""PreventOfflinePvPInterval"": ""0"",
      ""ResourcesRespawnPeriodMultiplier"": ""1"",
      ""bPvPDinoDecay"": ""False"",
      ""bPvPStructureDecay"": ""False"",
      ""DisableImprintDinoBuff"": ""False"",
      ""AllowAnyoneBabyImprintCuddle"": ""False"",
      ""EnableExtraStructurePreventionVolumes"": ""False"",
      ""ShowFloatingDamageText"": ""False"",
      ""DestroyUnconnectedWaterPipes"": ""False"",
      ""DisablePvEGamma"": ""False"",
      ""OverrideOfficialDifficulty"": ""0"",
      ""TheMaxStructuresInRange"": ""10500"",
      ""MinimumDinoReuploadInterval"": ""False"",
      ""PvEAllowStructuresAtSupplyDrops"": ""False"",
      ""NPCNetworkStasisRangeScalePlayerCountStart"": ""0"",
      ""NPCNetworkStasisRangeScalePlayerCountEnd"": ""0"",
      ""NPCNetworkStasisRangeScalePercentEnd"": ""0.55000001"",
      ""MaxPersonalTamedDinos"": ""0"",
      ""AutoDestroyDecayedDinos"": ""False"",
      ""ClampItemSpoilingTimes"": ""False"",
      ""UseOptimizedHarvestingHealth"": ""True"",
      ""AllowCrateSpawnsOnTopOfStructures"": ""False"",
      ""ForceFlyerExplosives"": ""False"",
      ""PreventOfflinePvP"": ""False"",
      ""AllowFlyingStaminaRecovery"": ""False"",
      ""OxygenSwimSpeedStatMultiplier"": ""1"",
      ""ServerAutoForceRespawnWildDinosInterval"": ""3600"",
      ""DisableWeatherFog"": ""False"",
      ""DayCycleSpeedScale"": ""1"",
      ""DayTimeSpeedScale"": ""1"",
      ""RandomSupplyCratePoints"": ""False"",
      ""PreventDiseases"": ""False"",
      ""PreventMateBoost"": ""False"",
      ""DontAlwaysNotifyPlayerJoined"": ""False"",
      ""ClampResourceHarvestDamage"": ""False"",
      ""CrossARKAllowForeignDinoDownloads"": ""False"",
      ""PersonalTamedDinosSaddleStructureCost"": ""19"",
      ""StructurePreventResourceRadiusMultiplier"": ""1"",
      ""StructureResistanceMultiplier"": ""1"",
      ""TribeNameChangeCooldown"": ""15"",
      ""PlayerCharacterHealthRecoveryMultiplier"": ""1"",
      ""PlayerCharacterStaminaDrainMultiplier"": ""1"",
      ""PlayerCharacterWaterDrainMultiplier"": ""1"",
      ""PlayerDamageMultiplier"": ""1"",
      ""PlayerResistanceMultiplier"": ""1"",
      ""PlatformSaddleBuildAreaBoundsMultiplier"": ""1"",
      ""AlwaysAllowStructurePickup"": ""False"",
      ""StructurePickupTimeAfterPlacement"": ""30"",
      ""StructurePickupHoldDuration"": ""1"",
      ""PlayerCharacterFoodDrainMultiplier"": ""1"",
      ""AllowHideDamageSourceFromLogs"": ""True"",
      ""RaidDinoCharacterFoodDrainMultiplier"": ""1"",
      ""DinoCharacterHealthRecoveryMultiplier"": ""1"",
      ""DinoCharacterStaminaDrainMultiplier"": ""1"",
      ""ItemStackSizeMultiplier"": ""1"",
      ""AllowMultipleAttachedC4"": ""False"",
      ""AllowRaidDinoFeeding"": ""False"",
      ""AllowHitMarkers"": ""True""
    },
    ""SessionSettings"": {
      ""SessionName"": """"
    },
    ""MessageOfTheDay"": {
      ""Message"": """"
    }
  }
}


";

                File.WriteAllText(jsonFilePath, defaultJsonContent);
            }
        }

        private void UpdateGameIniSettings(string gameIniPath)
        {
            try
            {
                var jsonString = File.ReadAllText(jsonFilePath);
                var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                var gameIniDefaults = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonObject["GameIniDefaults"].ToString());

                List<string> existingSettings = File.Exists(gameIniPath) ? File.ReadAllLines(gameIniPath).ToList() : new List<string>();

                foreach (var section in gameIniDefaults)
                {
                    var header = section["Header"].ToString();
                    if (!existingSettings.Contains($"[{header}]"))
                    {
                        existingSettings.Add($"[{header}]");
                    }

                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(section["Settings"].ToString());

                    foreach (var setting in settings)
                    {
                        if (!existingSettings.Any(line => line.StartsWith($"{setting.Key}=")))
                        {
                            existingSettings.Add($"{setting.Key}={setting.Value}");
                        }
                    }
                }

                File.WriteAllLines(gameIniPath, existingSettings);
            }
            catch (JsonException ex)
            {
                System.Windows.MessageBox.Show($"JSON error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }





        private void UpdateGameUserSettingsIniSettings(string gameUserSettingsIniPath)
        {
            try
            {
                var jsonString = File.ReadAllText(jsonFilePath);
                var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                var gameUserSettingsDefaults = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonObject["GameUserSettingsDefaults"].ToString());

                List<string> existingSettings = File.Exists(gameUserSettingsIniPath) ? File.ReadAllLines(gameUserSettingsIniPath).ToList() : new List<string>();

                foreach (var section in gameUserSettingsDefaults)
                {
                    var sectionKey = section.Key;
                    if (!existingSettings.Any(line => line.Trim() == $"[{sectionKey}]"))
                    {
                        existingSettings.Add($"[{sectionKey}]");
                    }

                    var settings = section.Value;

                    foreach (var setting in settings)
                    {
                        if (!existingSettings.Any(line => line.StartsWith($"{setting.Key}=")))
                        {
                            existingSettings.Add($"{setting.Key}={setting.Value}");
                        }
                    }
                }

                File.WriteAllLines(gameUserSettingsIniPath, existingSettings);
            }
            catch (JsonException ex)
            {
                System.Windows.MessageBox.Show($"JSON error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }


        public class ServerConfig
        {
            public string ProfileName { get; set; }
            public string ServerPath { get; set; }
            public string MapName { get; set; }
            public string AppId { get; set; }
            public string ServerName { get; set; }
            public int ListenPort { get; set; }
            public int RCONPort { get; set; }
            public List<string> Mods { get; set; }
            public int MaxPlayerCount { get; set; }
            public string AdminPassword { get; set; }
            public string ServerPassword { get; set; }
            public bool UseBattlEye { get; set; }
            public bool ForceRespawnDinos { get; set; }
            public bool PreventSpawnAnimation { get; set; }
        }
    }
}
