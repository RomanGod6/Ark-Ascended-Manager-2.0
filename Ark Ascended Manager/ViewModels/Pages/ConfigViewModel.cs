using System;
using System.Text.Json;
using Ark_Ascended_Manager.Models; // Ensure this is the correct namespace for ServerConfig
using static Ark_Ascended_Manager.Views.Pages.CreateServersPage;
using System.IO;
using System.Diagnostics;
using System.Windows.Input;

namespace Ark_Ascended_Manager.ViewModels.Pages
{
    public class ConfigPageViewModel : ObservableObject
    {
        public ServerConfig CurrentServerConfig { get; private set; }
        public ICommand SaveGameUserSettingsCommand { get; private set; }

        public ConfigPageViewModel()
        {
            LoadServerProfile();
            SaveGameUserSettingsCommand = new RelayCommand(SaveGameUserSettings);

        }

        private void LoadServerProfile()
        {
            // Define the path where the JSON is saved
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "currentServerConfig.json");

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON from the file
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON to a ServerConfig object
                CurrentServerConfig = JsonSerializer.Deserialize<ServerConfig>(json);

                // Now you can use CurrentServerConfig to get the profile name and find it in the server master list
                // For example:
                // string profileName = CurrentServerConfig.ProfileName;
                // ServerDetails details = FindInMasterList(profileName);
                // ...
            }
            LoadIniFile();
        }
        public void LoadConfig(ServerConfig serverConfig)
        {
            // Implementation to set up the ViewModel's properties based on serverConfig
            CurrentServerConfig = serverConfig;
            
            // ... Set other properties as needed
        }
        private void LoadIniFile()
        {
            Console.WriteLine("INI file load has been initiated.");
            if (CurrentServerConfig == null)
            {
                Console.WriteLine("CurrentServerConfig is null.");
                return;
            }

            string serverPath = CurrentServerConfig.ServerPath; // Assuming ServerPath is the correct property
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
            Console.WriteLine($"INI File Path: {iniFilePath}");

            if (!File.Exists(iniFilePath))
            {
                Console.WriteLine("INI file does not exist.");
                return;
            }

            var lines = File.ReadAllLines(iniFilePath);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith(";") && line.Contains("="))
                {
                    var keyValue = line.Split(new[] { '=' }, 2);
                    var key = keyValue[0].Trim();
                    var value = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;

                    switch (key)
                    {
                        case "HarvestAmountMultiplier":
                            HarvestAmountMultiplier = value;
                            break;
                        case "AllowThirdPersonPlayer":
                            AllowThirdPersonPlayer = Convert.ToBoolean(value);
                            break;
                        case "AllowCaveBuildingPvE":
                            AllowCaveBuildingPvE = Convert.ToBoolean(value);
                            break;
                        case "AlwaysNotifyPlayerJoined":
                            AlwaysNotifyPlayerJoined = Convert.ToBoolean(value);
                            break;
                        case "AlwaysNotifyPlayerLeft":
                            AlwaysNotifyPlayerLeft = Convert.ToBoolean(value);
                            break;
                        case "AllowFlyerCarryPvE":
                            AllowFlyerCarryPvE = Convert.ToBoolean(value);
                            break;
                        case "DisableStructureDecayPvE":
                            DisableStructureDecayPvE = Convert.ToBoolean(value);
                            break;
                        case "GlobalVoiceChat":
                            GlobalVoiceChat = Convert.ToBoolean(value);
                            break;
                        case "MaxStructuresInRange":
                            MaxStructuresInRange = value;
                            break;
                        case "NoTributeDownloads":
                            NoTributeDownloads = Convert.ToBoolean(value);
                            break;
                        case "PreventDownloadSurvivors":
                            PreventDownloadSurvivors = Convert.ToBoolean(value);
                            break;
                        case "PreventDownloadItems":
                            PreventDownloadItems = Convert.ToBoolean(value);
                            break;
                        case "PreventDownloadDinos":
                            PreventDownloadDinos = Convert.ToBoolean(value);
                            break;
                        case "ProximityChat":
                            ProximityChat = Convert.ToBoolean(value);
                            break;
                        case "ResourceNoReplenishRadiusStructures":
                            ResourceNoReplenishRadiusStructures = value;
                            break;
                        case "ServerAdminPassword":
                            ServerAdminPassword = value;
                            break;
                        case "ServerCrosshair":
                            ServerCrosshair = Convert.ToBoolean(value);
                            break;
                        case "ServerForceNoHud":
                            ServerForceNoHud = Convert.ToBoolean(value);
                            break;
                        case "ServerHardcore":
                            ServerHardcore = Convert.ToBoolean(value);
                            break;
                        case "ServerPassword":
                            ServerPassword = value;
                            break;
                        case "ServerPvE":
                            ServerPvE = Convert.ToBoolean(value);
                            break;
                        case "ShowMapPlayerLocation":
                            ShowMapPlayerLocation = Convert.ToBoolean(value);
                            break;
                        case "TamedDinoDamageMultiplier":
                            TamedDinoDamageMultiplier = value;
                            break;
                        case "TamedDinoResistanceMultiplier":
                            TamedDinoResistanceMultiplier = value;
                            break;
                        case "TamingSpeedMultiplier":
                            TamingSpeedMultiplier = value;
                            break;
                        case "XPMultiplier":
                            XPMultiplier = value;
                            break;
                        case "EnablePVPGamma":
                            EnablePVPGamma = Convert.ToBoolean(value);
                            break;
                        case "EnablePVEGamma":
                            EnablePVEGamma = Convert.ToBoolean(value);
                            break;
                        case "SpectatorPassword":
                            SpectatorPassword = value;
                            break;
                        case "DifficultyOffset":
                            DifficultyOffset = value;
                            break;
                        case "PvEStructureDecayDestructionPeriod":
                            PvEStructureDecayDestructionPeriod = value;
                            break;
                        case "Banlist":
                            Banlist = value;
                            break;
                        case "DisableDinoDecayPvE":
                            DisableDinoDecayPvE = Convert.ToBoolean(value);
                            break;
                        case "PvEDinoDecayPeriodMultiplier":
                            PvEDinoDecayPeriodMultiplier = value;
                            break;
                        case "AdminLogging":
                            AdminLogging = Convert.ToBoolean(value);
                            break;
                        case "MaxTamedDinos":
                            MaxTamedDinos = value;
                            break;
                        case "MaxNumbersofPlayersInTribe":
                            MaxNumbersofPlayersInTribe = value;
                            break;
                        case "BattleNumOfTribestoStartGame":
                            BattleNumOfTribestoStartGame = value;
                            break;
                        case "TimeToCollapseROD":
                            TimeToCollapseROD = value;
                            break;
                        case "BattleAutoStartGameInterval":
                            BattleAutoStartGameInterval = value;
                            break;
                        case "BattleSuddenDeathInterval":
                            BattleSuddenDeathInterval = value;
                            break;
                        case "KickIdlePlayersPeriod":
                            KickIdlePlayersPeriod = value;
                            break;
                        case "PerPlatformMaxStructuresMultiplier":
                            PerPlatformMaxStructuresMultiplier = value;
                            break;
                        case "ForceAllStructureLocking":
                            ForceAllStructureLocking = Convert.ToBoolean(value);
                            break;
                        case "AutoDestroyOldStructuresMultiplier":
                            AutoDestroyOldStructuresMultiplier = value;
                            break;
                        case "UseVSync":
                            UseVSync = Convert.ToBoolean(value);
                            break;
                        case "MaxPlatformSaddleStructureLimit":
                            MaxPlatformSaddleStructureLimit = value;
                            break;
                        case "PassiveDefensesDamageRiderlessDinos":
                            PassiveDefensesDamageRiderlessDinos = Convert.ToBoolean(value);
                            break;
                        case "RCONPort":
                            RCONPort = value;
                            break;
                        case "AutoSavePeriodMinutes":
                            AutoSavePeriodMinutes = value;
                            break;
                        case "RCONServerGameLogBuffer":
                            RCONServerGameLogBuffer = value;
                            break;
                        case "OverrideStructurePlatformPrevention":
                            OverrideStructurePlatformPrevention = Convert.ToBoolean(value);
                            break;
                        case "PreventOfflinePvPInterval":
                            PreventOfflinePvPInterval = value;
                            break;
                        case "bPvPDinoDecay":
                            BPvPDinoDecay = Convert.ToBoolean(value);
                            break;
                        case "bPvPStructureDecay":
                            BPvPStructureDecay = Convert.ToBoolean(value);
                            break;
                        case "DisableImprintDinoBuff":
                            DisableImprintDinoBuff = Convert.ToBoolean(value);
                            break;
                        case "AllowAnyoneBabyImprintCuddle":
                            AllowAnyoneBabyImprintCuddle = Convert.ToBoolean(value);
                            break;
                        case "EnableExtraStructurePreventionVolumes":
                            EnableExtraStructurePreventionVolumes = Convert.ToBoolean(value);
                            break;
                        case "ShowFloatingDamageText":
                            ShowFloatingDamageText = Convert.ToBoolean(value);
                            break;
                        case "DestroyUnconnectedWaterPipes":
                            DestroyUnconnectedWaterPipes = Convert.ToBoolean(value);
                            break;
                        case "OverrideOfficialDifficulty":
                            OverrideOfficialDifficulty = value;
                            break;
                        case "TheMaxStructuresInRange":
                            TheMaxStructuresInRange = value;
                            break;
                        case "MinimumDinoReuploadInterval":
                            MinimumDinoReuploadInterval = Convert.ToBoolean(value);
                            break;
                        case "PvEAllowStructuresAtSupplyDrops":
                            PvEAllowStructuresAtSupplyDrops = Convert.ToBoolean(value);
                            break;
                        case "NPCNetworkStasisRangeScalePlayerCountStart":
                            NPCNetworkStasisRangeScalePlayerCountStart = value;
                            break;
                        case "NPCNetworkStasisRangeScalePlayerCountEnd":
                            NPCNetworkStasisRangeScalePlayerCountEnd = value;
                            break;
                        case "NPCNetworkStasisRangeScalePercentEnd":
                            NPCNetworkStasisRangeScalePercentEnd = value;
                            break;
                        case "MaxPersonalTamedDinos":
                            MaxPersonalTamedDinos = value;
                            break;
                        case "AutoDestroyDecayedDinos":
                            AutoDestroyDecayedDinos = Convert.ToBoolean(value);
                            break;
                        case "ClampItemSpoilingTimes":
                            ClampItemSpoilingTimes = Convert.ToBoolean(value);
                            break;
                        case "UseOptimizedHarvestingHealth":
                            UseOptimizedHarvestingHealth = Convert.ToBoolean(value);
                            break;
                        case "AllowCrateSpawnsOnTopOfStructures":
                            AllowCrateSpawnsOnTopOfStructures = Convert.ToBoolean(value);
                            break;
                        case "ForceFlyerExplosives":
                            ForceFlyerExplosives = Convert.ToBoolean(value);
                            break;
                        case "AllowFlyingStaminaRecovery":
                            AllowFlyingStaminaRecovery = Convert.ToBoolean(value);
                            break;
                        case "OxygenSwimSpeedStatMultiplier":
                            OxygenSwimSpeedStatMultiplier = value;
                            break;
                        case "bPvEDisableFriendlyFire":
                            BPvEDisableFriendlyFire = Convert.ToBoolean(value);
                            break;
                        case "ServerAutoForceRespawnWildDinosInterval":
                            ServerAutoForceRespawnWildDinosInterval = value;
                            break;
                        case "DisableWeatherFog":
                            DisableWeatherFog = Convert.ToBoolean(value);
                            break;
                        case "RandomSupplyCratePoints":
                            RandomSupplyCratePoints = Convert.ToBoolean(value);
                            break;
                        case "CrossARKAllowForeignDinoDownloads":
                            CrossARKAllowForeignDinoDownloads = Convert.ToBoolean(value);
                            break;
                        case "PersonalTamedDinosSaddleStructureCost":
                            PersonalTamedDinosSaddleStructureCost = value;
                            break;
                        case "StructurePreventResourceRadiusMultiplier":
                            StructurePreventResourceRadiusMultiplier = value;
                            break;
                        case "TribeNameChangeCooldown":
                            TribeNameChangeCooldown = value;
                            break;
                        case "PlatformSaddleBuildAreaBoundsMultiplier":
                            PlatformSaddleBuildAreaBoundsMultiplier = value;
                            break;
                        case "AlwaysAllowStructurePickup":
                            AlwaysAllowStructurePickup = Convert.ToBoolean(value);
                            break;
                        case "StructurePickupTimeAfterPlacement":
                            StructurePickupTimeAfterPlacement = value;
                            break;
                        case "StructurePickupHoldDuration":
                            StructurePickupHoldDuration = value;
                            break;
                        case "AllowHideDamageSourceFromLogs":
                            AllowHideDamageSourceFromLogs = Convert.ToBoolean(value);
                            break;
                        case "RaidDinoCharacterFoodDrainMultiplier":
                            RaidDinoCharacterFoodDrainMultiplier = value;
                            break;
                        case "ItemStackSizeMultiplier":
                            ItemStackSizeMultiplier = value;
                            break;
                        case "AllowHitMarkers":
                            AllowHitMarkers = Convert.ToBoolean(value);
                            break;
                        case "AllowMultipleAttachedC4":
                            AllowMultipleAttachedC4 = Convert.ToBoolean(value);
                            break;





                    }
                }
            }
        }
        private void SaveGameUserSettings()
        {
            string serverPath = CurrentServerConfig.ServerPath;
            string iniFilePath = Path.Combine(serverPath, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");

            // Read all lines
            var lines = File.ReadAllLines(iniFilePath).ToList();

            // Update specific lines
            UpdateLine(ref lines, "HarvestAmountMultiplier", HarvestAmountMultiplier);
            UpdateLine(ref lines, "AllowThirdPersonPlayer", AllowThirdPersonPlayer.ToString());
            UpdateLine(ref lines, "AllowCaveBuildingPvE", AllowCaveBuildingPvE.ToString());
            UpdateLine(ref lines, "AlwaysNotifyPlayerJoined", AlwaysNotifyPlayerJoined.ToString());
            UpdateLine(ref lines, "AlwaysNotifyPlayerLeft", AlwaysNotifyPlayerLeft.ToString());
            UpdateLine(ref lines, "AllowFlyerCarryPvE", AllowFlyerCarryPvE.ToString());
            UpdateLine(ref lines, "DisableStructureDecayPvE", DisableStructureDecayPvE.ToString());
            UpdateLine(ref lines, "GlobalVoiceChat", GlobalVoiceChat.ToString());
            UpdateLine(ref lines, "MaxStructuresInRange", MaxStructuresInRange);
            UpdateLine(ref lines, "NoTributeDownloads", NoTributeDownloads.ToString());
            UpdateLine(ref lines, "PreventDownloadSurvivors", PreventDownloadSurvivors.ToString());
            UpdateLine(ref lines, "PreventDownloadItems", PreventDownloadItems.ToString());
            UpdateLine(ref lines, "PreventDownloadDinos", PreventDownloadDinos.ToString());
            UpdateLine(ref lines, "ProximityChat", ProximityChat.ToString());
            UpdateLine(ref lines, "ResourceNoReplenishRadiusStructures", ResourceNoReplenishRadiusStructures);
            UpdateLine(ref lines, "ServerAdminPassword", ServerAdminPassword);
            UpdateLine(ref lines, "ServerCrosshair", ServerCrosshair.ToString());
            UpdateLine(ref lines, "ServerForceNoHud", ServerForceNoHud.ToString());
            UpdateLine(ref lines, "ServerHardcore", ServerHardcore.ToString());
            UpdateLine(ref lines, "ServerPassword", ServerPassword);
            UpdateLine(ref lines, "ServerPvE", ServerPvE.ToString());
            UpdateLine(ref lines, "ShowMapPlayerLocation", ShowMapPlayerLocation.ToString());
            UpdateLine(ref lines, "TamedDinoDamageMultiplier", TamedDinoDamageMultiplier);
            UpdateLine(ref lines, "TamedDinoResistanceMultiplier", TamedDinoResistanceMultiplier);
            UpdateLine(ref lines, "TamingSpeedMultiplier", TamingSpeedMultiplier);
            UpdateLine(ref lines, "XPMultiplier", XPMultiplier);
            UpdateLine(ref lines, "EnablePVPGamma", EnablePVPGamma.ToString());
            UpdateLine(ref lines, "EnablePVEGamma", EnablePVEGamma.ToString());
            UpdateLine(ref lines, "AllowFlyingStaminaRecovery", AllowFlyingStaminaRecovery.ToString());
            UpdateLine(ref lines, "SpectatorPassword", SpectatorPassword);
            UpdateLine(ref lines, "DifficultyOffset", DifficultyOffset);
            UpdateLine(ref lines, "PvEStructureDecayDestructionPeriod", PvEStructureDecayDestructionPeriod);
            UpdateLine(ref lines, "Banlist", Banlist);
            UpdateLine(ref lines, "ServerAutoForceRespawnWildDinosInterval", ServerAutoForceRespawnWildDinosInterval);
            UpdateLine(ref lines, "DisableDinoDecayPvE", DisableDinoDecayPvE.ToString());
            UpdateLine(ref lines, "PvEDinoDecayPeriodMultiplier", PvEDinoDecayPeriodMultiplier);
            UpdateLine(ref lines, "AdminLogging", AdminLogging.ToString());
            UpdateLine(ref lines, "MaxTamedDinos", MaxTamedDinos);
            UpdateLine(ref lines, "MaxNumbersofPlayersInTribe", MaxNumbersofPlayersInTribe);
            UpdateLine(ref lines, "BattleNumOfTribestoStartGame", BattleNumOfTribestoStartGame);
            UpdateLine(ref lines, "TimeToCollapseROD", TimeToCollapseROD);
            UpdateLine(ref lines, "BattleAutoStartGameInterval", BattleAutoStartGameInterval);
            UpdateLine(ref lines, "BattleSuddenDeathInterval", BattleSuddenDeathInterval);
            UpdateLine(ref lines, "KickIdlePlayersPeriod", KickIdlePlayersPeriod);
            UpdateLine(ref lines, "PerPlatformMaxStructuresMultiplier", PerPlatformMaxStructuresMultiplier);
            UpdateLine(ref lines, "ForceAllStructureLocking", ForceAllStructureLocking.ToString());
            UpdateLine(ref lines, "AutoDestroyOldStructuresMultiplier", AutoDestroyOldStructuresMultiplier);
            UpdateLine(ref lines, "UseVSync", UseVSync.ToString());
            UpdateLine(ref lines, "MaxPlatformSaddleStructureLimit", MaxPlatformSaddleStructureLimit);
            UpdateLine(ref lines, "PassiveDefensesDamageRiderlessDinos", PassiveDefensesDamageRiderlessDinos.ToString());
            UpdateLine(ref lines, "RCONPort", RCONPort);
            UpdateLine(ref lines, "bPvEDisableFriendlyFire", BPvEDisableFriendlyFire.ToString());
            UpdateLine(ref lines, "AutoSavePeriodMinutes", AutoSavePeriodMinutes);
            UpdateLine(ref lines, "RCONServerGameLogBuffer", RCONServerGameLogBuffer);
            UpdateLine(ref lines, "OverrideStructurePlatformPrevention", OverrideStructurePlatformPrevention.ToString());
            UpdateLine(ref lines, "bPvPDinoDecay", BPvPDinoDecay.ToString());
            UpdateLine(ref lines, "bPvPStructureDecay", BPvPStructureDecay.ToString());
            UpdateLine(ref lines, "DisableImprintDinoBuff", DisableImprintDinoBuff.ToString());
            UpdateLine(ref lines, "AllowAnyoneBabyImprintCuddle", AllowAnyoneBabyImprintCuddle.ToString());
            UpdateLine(ref lines, "EnableExtraStructurePreventionVolumes", EnableExtraStructurePreventionVolumes.ToString());
            UpdateLine(ref lines, "ShowFloatingDamageText", ShowFloatingDamageText.ToString());
            UpdateLine(ref lines, "DestroyUnconnectedWaterPipes", DestroyUnconnectedWaterPipes.ToString());
            UpdateLine(ref lines, "OverrideOfficialDifficulty", OverrideOfficialDifficulty);
            UpdateLine(ref lines, "TheMaxStructuresInRange", TheMaxStructuresInRange);
            UpdateLine(ref lines, "MinimumDinoReuploadInterval", MinimumDinoReuploadInterval.ToString());
            UpdateLine(ref lines, "PvEAllowStructuresAtSupplyDrops", PvEAllowStructuresAtSupplyDrops.ToString());
            UpdateLine(ref lines, "NPCNetworkStasisRangeScalePlayerCountStart", NPCNetworkStasisRangeScalePlayerCountStart);
            UpdateLine(ref lines, "NPCNetworkStasisRangeScalePlayerCountEnd", NPCNetworkStasisRangeScalePlayerCountEnd);
            UpdateLine(ref lines, "NPCNetworkStasisRangeScalePercentEnd", NPCNetworkStasisRangeScalePercentEnd);
            UpdateLine(ref lines, "MaxPersonalTamedDinos", MaxPersonalTamedDinos);
            UpdateLine(ref lines, "PreventOfflinePvPInterval", PreventOfflinePvPInterval);
            UpdateLine(ref lines, "AutoDestroyDecayedDinos", AutoDestroyDecayedDinos.ToString());
            UpdateLine(ref lines, "ClampItemSpoilingTimes", ClampItemSpoilingTimes.ToString());
            UpdateLine(ref lines, "UseOptimizedHarvestingHealth", UseOptimizedHarvestingHealth.ToString());
            UpdateLine(ref lines, "AllowCrateSpawnsOnTopOfStructures", AllowCrateSpawnsOnTopOfStructures.ToString());
            UpdateLine(ref lines, "ForceFlyerExplosives", ForceFlyerExplosives.ToString());
            UpdateLine(ref lines, "AllowMultipleAttachedC4", AllowMultipleAttachedC4.ToString());
            UpdateLine(ref lines, "DisableWeatherFog", DisableWeatherFog.ToString());
            UpdateLine(ref lines, "RandomSupplyCratePoints", RandomSupplyCratePoints.ToString());
            UpdateLine(ref lines, "CrossARKAllowForeignDinoDownloads", CrossARKAllowForeignDinoDownloads.ToString());  
            UpdateLine(ref lines, "AlwaysAllowStructurePickup", AlwaysAllowStructurePickup.ToString());
            UpdateLine(ref lines, "AllowHideDamageSourceFromLogs", AllowHideDamageSourceFromLogs.ToString());
            UpdateLine(ref lines, "AllowHitMarkers", AllowHitMarkers.ToString());
            UpdateLine(ref lines, "OxygenSwimSpeedStatMultiplier", OxygenSwimSpeedStatMultiplier);
            UpdateLine(ref lines, "PersonalTamedDinosSaddleStructureCost", PersonalTamedDinosSaddleStructureCost);
            UpdateLine(ref lines, "StructurePreventResourceRadiusMultiplier", StructurePreventResourceRadiusMultiplier);
            UpdateLine(ref lines, "TribeNameChangeCooldown", TribeNameChangeCooldown);
            UpdateLine(ref lines, "PlatformSaddleBuildAreaBoundsMultiplier", PlatformSaddleBuildAreaBoundsMultiplier);
            UpdateLine(ref lines, "StructurePickupHoldDuration", StructurePickupHoldDuration);
            UpdateLine(ref lines, "StructurePickupTimeAfterPlacement", StructurePickupTimeAfterPlacement);
            UpdateLine(ref lines, "RaidDinoCharacterFoodDrainMultiplier", RaidDinoCharacterFoodDrainMultiplier);

            // ... Repeat for other properties ...

           // Write the updated lines back to the file
           File.WriteAllLines(iniFilePath, lines);
        }

        private void UpdateLine(ref List<string> lines, string key, string newValue)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith(key))
                {
                    lines[i] = $"{key}={newValue}";
                    return;
                }
            }

            // If the key does not exist in the file, add it
            lines.Add($"{key}={newValue}");
        }



        private string _harvestAmountMultiplier;
        public string HarvestAmountMultiplier
        {
            get { return _harvestAmountMultiplier; }
            set
            {
                _harvestAmountMultiplier = value;
                OnPropertyChanged(nameof(HarvestAmountMultiplier)); // Notify the UI of the change
            }
        }
        private bool _allowThirdPersonPlayer;
        public bool AllowThirdPersonPlayer
        {
            get { return _allowThirdPersonPlayer; }
            set
            {
                _allowThirdPersonPlayer = value;
                OnPropertyChanged(nameof(AllowThirdPersonPlayer)); // Notify the UI of the change
            }
        }

        private bool _allowCaveBuildingPvE;
        public bool AllowCaveBuildingPvE
        {
            get { return _allowCaveBuildingPvE; }
            set
            {
                _allowCaveBuildingPvE = value;
                OnPropertyChanged(nameof(AllowCaveBuildingPvE)); // Notify the UI of the change
            }
        }

        private bool _alwaysNotifyPlayerJoined;
        public bool AlwaysNotifyPlayerJoined
        {
            get { return _alwaysNotifyPlayerJoined; }
            set
            {
                _alwaysNotifyPlayerJoined = value;
                OnPropertyChanged(nameof(AlwaysNotifyPlayerJoined)); // Notify the UI of the change
            }
        }

        private bool _alwaysNotifyPlayerLeft;
        public bool AlwaysNotifyPlayerLeft
        {
            get { return _alwaysNotifyPlayerLeft; }
            set
            {
                _alwaysNotifyPlayerLeft = value;
                OnPropertyChanged(nameof(AlwaysNotifyPlayerLeft)); // Notify the UI of the change
            }
        }

        private bool _allowFlyerCarryPvE;
        public bool AllowFlyerCarryPvE
        {
            get { return _allowFlyerCarryPvE; }
            set
            {
                _allowFlyerCarryPvE = value;
                OnPropertyChanged(nameof(AllowFlyerCarryPvE)); // Notify the UI of the change
            }
        }

        private bool _disableStructureDecayPvE;
        public bool DisableStructureDecayPvE
        {
            get { return _disableStructureDecayPvE; }
            set
            {
                _disableStructureDecayPvE = value;
                OnPropertyChanged(nameof(DisableStructureDecayPvE)); // Notify the UI of the change
            }
        }

        private bool _globalVoiceChat;
        public bool GlobalVoiceChat
        {
            get { return _globalVoiceChat; }
            set
            {
                _globalVoiceChat = value;
                OnPropertyChanged(nameof(GlobalVoiceChat)); // Notify the UI of the change
            }
        }

        private string _maxStructuresInRange;
        public string MaxStructuresInRange
        {
            get { return _maxStructuresInRange; }
            set
            {
                _maxStructuresInRange = value;
                OnPropertyChanged(nameof(MaxStructuresInRange)); // Notify the UI of the change
            }
        }

        private bool _noTributeDownloads;
        public bool NoTributeDownloads
        {
            get { return _noTributeDownloads; }
            set
            {
                _noTributeDownloads = value;
                OnPropertyChanged(nameof(NoTributeDownloads)); // Notify the UI of the change
            }
        }

        private bool _preventDownloadSurvivors;
        public bool PreventDownloadSurvivors
        {
            get { return _preventDownloadSurvivors; }
            set
            {
                _preventDownloadSurvivors = value;
                OnPropertyChanged(nameof(PreventDownloadSurvivors)); // Notify the UI of the change
            }
        }

        private bool _preventDownloadItems;
        public bool PreventDownloadItems
        {
            get { return _preventDownloadItems; }
            set
            {
                _preventDownloadItems = value;
                OnPropertyChanged(nameof(PreventDownloadItems)); // Notify the UI of the change
            }
        }

        private bool _preventDownloadDinos;
        public bool PreventDownloadDinos
        {
            get { return _preventDownloadDinos; }
            set
            {
                _preventDownloadDinos = value;
                OnPropertyChanged(nameof(PreventDownloadDinos)); // Notify the UI of the change
            }
        }

        private bool _proximityChat;
        public bool ProximityChat
        {
            get { return _proximityChat; }
            set
            {
                _proximityChat = value;
                OnPropertyChanged(nameof(ProximityChat)); // Notify the UI of the change
            }
        }

        private string _resourceNoReplenishRadiusStructures;
        public string ResourceNoReplenishRadiusStructures
        {
            get { return _resourceNoReplenishRadiusStructures; }
            set
            {
                _resourceNoReplenishRadiusStructures = value;
                OnPropertyChanged(nameof(ResourceNoReplenishRadiusStructures)); // Notify the UI of the change
            }
        }

        private string _serverAdminPassword;
        public string ServerAdminPassword
        {
            get { return _serverAdminPassword; }
            set
            {
                _serverAdminPassword = value;
                OnPropertyChanged(nameof(ServerAdminPassword)); // Notify the UI of the change
            }
        }

        private bool _serverCrosshair;
        public bool ServerCrosshair
        {
            get { return _serverCrosshair; }
            set
            {
                _serverCrosshair = value;
                OnPropertyChanged(nameof(ServerCrosshair)); // Notify the UI of the change
            }
        }

        private bool _serverForceNoHud;
        public bool ServerForceNoHud
        {
            get { return _serverForceNoHud; }
            set
            {
                _serverForceNoHud = value;
                OnPropertyChanged(nameof(ServerForceNoHud)); // Notify the UI of the change
            }
        }

        private bool _serverHardcore;
        public bool ServerHardcore
        {
            get { return _serverHardcore; }
            set
            {
                _serverHardcore = value;
                OnPropertyChanged(nameof(ServerHardcore)); // Notify the UI of the change
            }
        }

        private string _serverPassword;
        public string ServerPassword
        {
            get { return _serverPassword; }
            set
            {
                _serverPassword = value;
                OnPropertyChanged(nameof(ServerPassword)); // Notify the UI of the change
            }
        }

        private bool _serverPvE;
        public bool ServerPvE
        {
            get { return _serverPvE; }
            set
            {
                _serverPvE = value;
                OnPropertyChanged(nameof(ServerPvE)); // Notify the UI of the change
            }
        }

        private bool _showMapPlayerLocation;
        public bool ShowMapPlayerLocation
        {
            get { return _showMapPlayerLocation; }
            set
            {
                _showMapPlayerLocation = value;
                OnPropertyChanged(nameof(ShowMapPlayerLocation)); // Notify the UI of the change
            }
        }

        private string _tamedDinoDamageMultiplier;
        public string TamedDinoDamageMultiplier
        {
            get { return _tamedDinoDamageMultiplier; }
            set
            {
                _tamedDinoDamageMultiplier = value;
                OnPropertyChanged(nameof(TamedDinoDamageMultiplier)); // Notify the UI of the change
            }
        }

        private string _tamedDinoResistanceMultiplier;
        public string TamedDinoResistanceMultiplier
        {
            get { return _tamedDinoResistanceMultiplier; }
            set
            {
                _tamedDinoResistanceMultiplier = value;
                OnPropertyChanged(nameof(TamedDinoResistanceMultiplier)); // Notify the UI of the change
            }
        }

        private string _tamingSpeedMultiplier;
        public string TamingSpeedMultiplier
        {
            get { return _tamingSpeedMultiplier; }
            set
            {
                _tamingSpeedMultiplier = value;
                OnPropertyChanged(nameof(TamingSpeedMultiplier)); // Notify the UI of the change
            }
        }

        private string _xpMultiplier;
        public string XPMultiplier
        {
            get { return _xpMultiplier; }
            set
            {
                _xpMultiplier = value;
                OnPropertyChanged(nameof(XPMultiplier)); // Notify the UI of the change
            }
        }

        private bool _enablePVPGamma;
        public bool EnablePVPGamma
        {
            get { return _enablePVPGamma; }
            set
            {
                _enablePVPGamma = value;
                OnPropertyChanged(nameof(EnablePVPGamma)); // Notify the UI of the change
            }
        }

        private bool _enablePVEGamma;
        public bool EnablePVEGamma
        {
            get { return _enablePVEGamma; }
            set
            {
                _enablePVEGamma = value;
                OnPropertyChanged(nameof(EnablePVEGamma)); // Notify the UI of the change
            }
        }

        private string _spectatorPassword;
        public string SpectatorPassword
        {
            get { return _spectatorPassword; }
            set
            {
                _spectatorPassword = value;
                OnPropertyChanged(nameof(SpectatorPassword)); // Notify the UI of the change
            }
        }

        private string _difficultyOffset;
        public string DifficultyOffset
        {
            get { return _difficultyOffset; }
            set
            {
                _difficultyOffset = value;
                OnPropertyChanged(nameof(DifficultyOffset)); // Notify the UI of the change
            }
        }

        private string _pveStructureDecayDestructionPeriod;
        public string PvEStructureDecayDestructionPeriod
        {
            get { return _pveStructureDecayDestructionPeriod; }
            set
            {
                _pveStructureDecayDestructionPeriod = value;
                OnPropertyChanged(nameof(PvEStructureDecayDestructionPeriod)); // Notify the UI of the change
            }
        }

        private string _banlist;
        public string Banlist
        {
            get { return _banlist; }
            set
            {
                _banlist = value;
                OnPropertyChanged(nameof(Banlist)); // Notify the UI of the change
            }
        }



        private bool _disableDinoDecayPvE;
        public bool DisableDinoDecayPvE
        {
            get { return _disableDinoDecayPvE; }
            set
            {
                _disableDinoDecayPvE = value;
                OnPropertyChanged(nameof(DisableDinoDecayPvE)); // Notify the UI of the change
            }
        }

        private string _pveDinoDecayPeriodMultiplier;
        public string PvEDinoDecayPeriodMultiplier
        {
            get { return _pveDinoDecayPeriodMultiplier; }
            set
            {
                _pveDinoDecayPeriodMultiplier = value;
                OnPropertyChanged(nameof(PvEDinoDecayPeriodMultiplier)); // Notify the UI of the change
            }
        }

        private bool _adminLogging;
        public bool AdminLogging
        {
            get { return _adminLogging; }
            set
            {
                _adminLogging = value;
                OnPropertyChanged(nameof(AdminLogging)); // Notify the UI of the change
            }
        }

        private string _maxTamedDinos;
        public string MaxTamedDinos
        {
            get { return _maxTamedDinos; }
            set
            {
                _maxTamedDinos = value;
                OnPropertyChanged(nameof(MaxTamedDinos)); // Notify the UI of the change
            }
        }

        private string _maxNumbersofPlayersInTribe;
        public string MaxNumbersofPlayersInTribe
        {
            get { return _maxNumbersofPlayersInTribe; }
            set
            {
                _maxNumbersofPlayersInTribe = value;
                OnPropertyChanged(nameof(MaxNumbersofPlayersInTribe)); // Notify the UI of the change
            }
        }

        private string _battleNumOfTribestoStartGame;
        public string BattleNumOfTribestoStartGame
        {
            get { return _battleNumOfTribestoStartGame; }
            set
            {
                _battleNumOfTribestoStartGame = value;
                OnPropertyChanged(nameof(BattleNumOfTribestoStartGame)); // Notify the UI of the change
            }
        }

        private string _timeToCollapseROD;
        public string TimeToCollapseROD
        {
            get { return _timeToCollapseROD; }
            set
            {
                _timeToCollapseROD = value;
                OnPropertyChanged(nameof(TimeToCollapseROD)); // Notify the UI of the change
            }
        }

        private string _battleAutoStartGameInterval;
        public string BattleAutoStartGameInterval
        {
            get { return _battleAutoStartGameInterval; }
            set
            {
                _battleAutoStartGameInterval = value;
                OnPropertyChanged(nameof(BattleAutoStartGameInterval)); // Notify the UI of the change
            }
        }

        private string _battleSuddenDeathInterval;
        public string BattleSuddenDeathInterval
        {
            get { return _battleSuddenDeathInterval; }
            set
            {
                _battleSuddenDeathInterval = value;
                OnPropertyChanged(nameof(BattleSuddenDeathInterval)); // Notify the UI of the change
            }
        }

        private string _kickIdlePlayersPeriod;
        public string KickIdlePlayersPeriod
        {
            get { return _kickIdlePlayersPeriod; }
            set
            {
                _kickIdlePlayersPeriod = value;
                OnPropertyChanged(nameof(KickIdlePlayersPeriod)); // Notify the UI of the change
            }
        }

        private string _perPlatformMaxStructuresMultiplier;
        public string PerPlatformMaxStructuresMultiplier
        {
            get { return _perPlatformMaxStructuresMultiplier; }
            set
            {
                _perPlatformMaxStructuresMultiplier = value;
                OnPropertyChanged(nameof(PerPlatformMaxStructuresMultiplier)); // Notify the UI of the change
            }
        }

        private bool _forceAllStructureLocking;
        public bool ForceAllStructureLocking
        {
            get { return _forceAllStructureLocking; }
            set
            {
                _forceAllStructureLocking = value;
                OnPropertyChanged(nameof(ForceAllStructureLocking)); // Notify the UI of the change
            }
        }
        private string _autoDestroyOldStructuresMultiplier;
        public string AutoDestroyOldStructuresMultiplier
        {
            get { return _autoDestroyOldStructuresMultiplier; }
            set
            {
                _autoDestroyOldStructuresMultiplier = value;
                OnPropertyChanged(nameof(AutoDestroyOldStructuresMultiplier)); // Notify the UI of the change
            }
        }

        private bool _useVSync;
        public bool UseVSync
        {
            get { return _useVSync; }
            set
            {
                _useVSync = value;
                OnPropertyChanged(nameof(UseVSync)); // Notify the UI of the change
            }
        }

        private string _maxPlatformSaddleStructureLimit;
        public string MaxPlatformSaddleStructureLimit
        {
            get { return _maxPlatformSaddleStructureLimit; }
            set
            {
                _maxPlatformSaddleStructureLimit = value;
                OnPropertyChanged(nameof(MaxPlatformSaddleStructureLimit)); // Notify the UI of the change
            }
        }

        private bool _passiveDefensesDamageRiderlessDinos;
        public bool PassiveDefensesDamageRiderlessDinos
        {
            get { return _passiveDefensesDamageRiderlessDinos; }
            set
            {
                _passiveDefensesDamageRiderlessDinos = value;
                OnPropertyChanged(nameof(PassiveDefensesDamageRiderlessDinos)); // Notify the UI of the change
            }
        }

        private string _rconPort;
        public string RCONPort
        {
            get { return _rconPort; }
            set
            {
                _rconPort = value;
                OnPropertyChanged(nameof(RCONPort)); // Notify the UI of the change
            }
        }

        private string _autoSavePeriodMinutes;
        public string AutoSavePeriodMinutes
        {
            get { return _autoSavePeriodMinutes; }
            set
            {
                _autoSavePeriodMinutes = value;
                OnPropertyChanged(nameof(AutoSavePeriodMinutes)); // Notify the UI of the change
            }
        }

        private string _rconServerGameLogBuffer;
        public string RCONServerGameLogBuffer
        {
            get { return _rconServerGameLogBuffer; }
            set
            {
                _rconServerGameLogBuffer = value;
                OnPropertyChanged(nameof(RCONServerGameLogBuffer)); // Notify the UI of the change
            }
        }

        private bool _overrideStructurePlatformPrevention;
        public bool OverrideStructurePlatformPrevention
        {
            get { return _overrideStructurePlatformPrevention; }
            set
            {
                _overrideStructurePlatformPrevention = value;
                OnPropertyChanged(nameof(OverrideStructurePlatformPrevention)); // Notify the UI of the change
            }
        }

        private string _preventOfflinePvPInterval;
        public string PreventOfflinePvPInterval
        {
            get { return _preventOfflinePvPInterval; }
            set
            {
                _preventOfflinePvPInterval = value;
                OnPropertyChanged(nameof(PreventOfflinePvPInterval)); // Notify the UI of the change
            }
        }

        private bool _bPvPDinoDecay;
        public bool BPvPDinoDecay
        {
            get { return _bPvPDinoDecay; }
            set
            {
                _bPvPDinoDecay = value;
                OnPropertyChanged(nameof(BPvPDinoDecay)); // Notify the UI of the change
            }
        }

        private bool _bPvPStructureDecay;
        public bool BPvPStructureDecay
        {
            get { return _bPvPStructureDecay; }
            set
            {
                _bPvPStructureDecay = value;
                OnPropertyChanged(nameof(BPvPStructureDecay)); // Notify the UI of the change
            }
        }

        private bool _disableImprintDinoBuff;
        public bool DisableImprintDinoBuff
        {
            get { return _disableImprintDinoBuff; }
            set
            {
                _disableImprintDinoBuff = value;
                OnPropertyChanged(nameof(DisableImprintDinoBuff)); // Notify the UI of the change
            }
        }

        private bool _allowAnyoneBabyImprintCuddle;
        public bool AllowAnyoneBabyImprintCuddle
        {
            get { return _allowAnyoneBabyImprintCuddle; }
            set
            {
                _allowAnyoneBabyImprintCuddle = value;
                OnPropertyChanged(nameof(AllowAnyoneBabyImprintCuddle)); // Notify the UI of the change
            }
        }

        private bool _enableExtraStructurePreventionVolumes;
        public bool EnableExtraStructurePreventionVolumes
        {
            get { return _enableExtraStructurePreventionVolumes; }
            set
            {
                _enableExtraStructurePreventionVolumes = value;
                OnPropertyChanged(nameof(EnableExtraStructurePreventionVolumes)); // Notify the UI of the change
            }
        }

        private bool _showFloatingDamageText;
        public bool ShowFloatingDamageText
        {
            get { return _showFloatingDamageText; }
            set
            {
                _showFloatingDamageText = value;
                OnPropertyChanged(nameof(ShowFloatingDamageText)); // Notify the UI of the change
            }
        }

        private bool _destroyUnconnectedWaterPipes;
        public bool DestroyUnconnectedWaterPipes
        {
            get { return _destroyUnconnectedWaterPipes; }
            set
            {
                _destroyUnconnectedWaterPipes = value;
                OnPropertyChanged(nameof(DestroyUnconnectedWaterPipes)); // Notify the UI of the change
            }
        }

        private string _overrideOfficialDifficulty;
        public string OverrideOfficialDifficulty
        {
            get { return _overrideOfficialDifficulty; }
            set
            {
                _overrideOfficialDifficulty = value;
                OnPropertyChanged(nameof(OverrideOfficialDifficulty)); // Notify the UI of the change
            }
        }

        private string _theMaxStructuresInRange;
        public string TheMaxStructuresInRange
        {
            get { return _theMaxStructuresInRange; }
            set
            {
                _theMaxStructuresInRange = value;
                OnPropertyChanged(nameof(TheMaxStructuresInRange)); // Notify the UI of the change
            }
        }

        private bool _minimumDinoReuploadInterval;
        public bool MinimumDinoReuploadInterval
        {
            get { return _minimumDinoReuploadInterval; }
            set
            {
                _minimumDinoReuploadInterval = value;
                OnPropertyChanged(nameof(MinimumDinoReuploadInterval)); // Notify the UI of the change
            }
        }

        private bool _pvEAllowStructuresAtSupplyDrops;
        public bool PvEAllowStructuresAtSupplyDrops
        {
            get { return _pvEAllowStructuresAtSupplyDrops; }
            set
            {
                _pvEAllowStructuresAtSupplyDrops = value;
                OnPropertyChanged(nameof(PvEAllowStructuresAtSupplyDrops)); // Notify the UI of the change
            }
        }

        private string _nPCNetworkStasisRangeScalePlayerCountStart;
        public string NPCNetworkStasisRangeScalePlayerCountStart
        {
            get { return _nPCNetworkStasisRangeScalePlayerCountStart; }
            set
            {
                _nPCNetworkStasisRangeScalePlayerCountStart = value;
                OnPropertyChanged(nameof(NPCNetworkStasisRangeScalePlayerCountStart)); // Notify the UI of the change
            }
        }

        private string _nPCNetworkStasisRangeScalePlayerCountEnd;
        public string NPCNetworkStasisRangeScalePlayerCountEnd
        {
            get { return _nPCNetworkStasisRangeScalePlayerCountEnd; }
            set
            {
                _nPCNetworkStasisRangeScalePlayerCountEnd = value;
                OnPropertyChanged(nameof(NPCNetworkStasisRangeScalePlayerCountEnd)); // Notify the UI of the change
            }
        }

        private string _nPCNetworkStasisRangeScalePercentEnd;
        public string NPCNetworkStasisRangeScalePercentEnd
        {
            get { return _nPCNetworkStasisRangeScalePercentEnd; }
            set
            {
                _nPCNetworkStasisRangeScalePercentEnd = value;
                OnPropertyChanged(nameof(NPCNetworkStasisRangeScalePercentEnd)); // Notify the UI of the change
            }
        }

        private string _maxPersonalTamedDinos;
        public string MaxPersonalTamedDinos
        {
            get { return _maxPersonalTamedDinos; }
            set
            {
                _maxPersonalTamedDinos = value;
                OnPropertyChanged(nameof(MaxPersonalTamedDinos)); // Notify the UI of the change
            }
        }

        private bool _autoDestroyDecayedDinos;
        public bool AutoDestroyDecayedDinos
        {
            get { return _autoDestroyDecayedDinos; }
            set
            {
                _autoDestroyDecayedDinos = value;
                OnPropertyChanged(nameof(AutoDestroyDecayedDinos)); // Notify the UI of the change
            }
        }

        private bool _clampItemSpoilingTimes;
        public bool ClampItemSpoilingTimes
        {
            get { return _clampItemSpoilingTimes; }
            set
            {
                _clampItemSpoilingTimes = value;
                OnPropertyChanged(nameof(ClampItemSpoilingTimes)); // Notify the UI of the change
            }
        }

        private bool _useOptimizedHarvestingHealth;
        public bool UseOptimizedHarvestingHealth
        {
            get { return _useOptimizedHarvestingHealth; }
            set
            {
                _useOptimizedHarvestingHealth = value;
                OnPropertyChanged(nameof(UseOptimizedHarvestingHealth)); // Notify the UI of the change
            }
        }

        private bool _allowCrateSpawnsOnTopOfStructures;
        public bool AllowCrateSpawnsOnTopOfStructures
        {
            get { return _allowCrateSpawnsOnTopOfStructures; }
            set
            {
                _allowCrateSpawnsOnTopOfStructures = value;
                OnPropertyChanged(nameof(AllowCrateSpawnsOnTopOfStructures)); // Notify the UI of the change
            }
        }

        private bool _forceFlyerExplosives;
        public bool ForceFlyerExplosives
        {
            get { return _forceFlyerExplosives; }
            set
            {
                _forceFlyerExplosives = value;
                OnPropertyChanged(nameof(ForceFlyerExplosives)); // Notify the UI of the change
            }
        }



        private bool _allowFlyingStaminaRecovery;
        public bool AllowFlyingStaminaRecovery
        {
            get { return _allowFlyingStaminaRecovery; }
            set
            {
                _allowFlyingStaminaRecovery = value;
                OnPropertyChanged(nameof(AllowFlyingStaminaRecovery)); // Notify the UI of the change
            }
        }

        private string _oxygenSwimSpeedStatMultiplier;
        public string OxygenSwimSpeedStatMultiplier
        {
            get { return _oxygenSwimSpeedStatMultiplier; }
            set
            {
                _oxygenSwimSpeedStatMultiplier = value;
                OnPropertyChanged(nameof(OxygenSwimSpeedStatMultiplier)); // Notify the UI of the change
            }
        }

        private bool _bPvEDisableFriendlyFire;
        public bool BPvEDisableFriendlyFire
        {
            get { return _bPvEDisableFriendlyFire; }
            set
            {
                _bPvEDisableFriendlyFire = value;
                OnPropertyChanged(nameof(BPvEDisableFriendlyFire)); // Notify the UI of the change
            }
        }

        private string _serverAutoForceRespawnWildDinosInterval;
        public string ServerAutoForceRespawnWildDinosInterval
        {
            get { return _serverAutoForceRespawnWildDinosInterval; }
            set
            {
                _serverAutoForceRespawnWildDinosInterval = value;
                OnPropertyChanged(nameof(ServerAutoForceRespawnWildDinosInterval)); // Notify the UI of the change
            }
        }

        private bool _disableWeatherFog;
        public bool DisableWeatherFog
        {
            get { return _disableWeatherFog; }
            set
            {
                _disableWeatherFog = value;
                OnPropertyChanged(nameof(DisableWeatherFog)); // Notify the UI of the change
            }
        }

        private bool _randomSupplyCratePoints;
        public bool RandomSupplyCratePoints
        {
            get { return _randomSupplyCratePoints; }
            set
            {
                _randomSupplyCratePoints = value;
                OnPropertyChanged(nameof(RandomSupplyCratePoints)); // Notify the UI of the change
            }
        }

        private bool _crossARKAllowForeignDinoDownloads;
        public bool CrossARKAllowForeignDinoDownloads
        {
            get { return _crossARKAllowForeignDinoDownloads; }
            set
            {
                _crossARKAllowForeignDinoDownloads = value;
                OnPropertyChanged(nameof(CrossARKAllowForeignDinoDownloads)); // Notify the UI of the change
            }
        }

        private string _personalTamedDinosSaddleStructureCost;
        public string PersonalTamedDinosSaddleStructureCost
        {
            get { return _personalTamedDinosSaddleStructureCost; }
            set
            {
                _personalTamedDinosSaddleStructureCost = value;
                OnPropertyChanged(nameof(PersonalTamedDinosSaddleStructureCost)); // Notify the UI of the change
            }
        }

        private string _structurePreventResourceRadiusMultiplier;
        public string StructurePreventResourceRadiusMultiplier
        {
            get { return _structurePreventResourceRadiusMultiplier; }
            set
            {
                _structurePreventResourceRadiusMultiplier = value;
                OnPropertyChanged(nameof(StructurePreventResourceRadiusMultiplier)); // Notify the UI of the change
            }
        }
        private string _tribeNameChangeCooldown;
        public string TribeNameChangeCooldown
        {
            get { return _tribeNameChangeCooldown; }
            set
            {
                _tribeNameChangeCooldown = value;
                OnPropertyChanged(nameof(TribeNameChangeCooldown)); // Notify the UI of the change
            }
        }

        private string _platformSaddleBuildAreaBoundsMultiplier;
        public string PlatformSaddleBuildAreaBoundsMultiplier
        {
            get { return _platformSaddleBuildAreaBoundsMultiplier; }
            set
            {
                _platformSaddleBuildAreaBoundsMultiplier = value;
                OnPropertyChanged(nameof(PlatformSaddleBuildAreaBoundsMultiplier)); // Notify the UI of the change
            }
        }

        private bool _alwaysAllowStructurePickup;
        public bool AlwaysAllowStructurePickup
        {
            get { return _alwaysAllowStructurePickup; }
            set
            {
                _alwaysAllowStructurePickup = value;
                OnPropertyChanged(nameof(AlwaysAllowStructurePickup)); // Notify the UI of the change
            }
        }

        private string _structurePickupTimeAfterPlacement;
        public string StructurePickupTimeAfterPlacement
        {
            get { return _structurePickupTimeAfterPlacement; }
            set
            {
                _structurePickupTimeAfterPlacement = value;
                OnPropertyChanged(nameof(StructurePickupTimeAfterPlacement)); // Notify the UI of the change
            }
        }

        private string _structurePickupHoldDuration;
        public string StructurePickupHoldDuration
        {
            get { return _structurePickupHoldDuration; }
            set
            {
                _structurePickupHoldDuration = value;
                OnPropertyChanged(nameof(StructurePickupHoldDuration)); // Notify the UI of the change
            }
        }

        private bool _allowHideDamageSourceFromLogs;
        public bool AllowHideDamageSourceFromLogs
        {
            get { return _allowHideDamageSourceFromLogs; }
            set
            {
                _allowHideDamageSourceFromLogs = value;
                OnPropertyChanged(nameof(AllowHideDamageSourceFromLogs)); // Notify the UI of the change
            }
        }

        private string _raidDinoCharacterFoodDrainMultiplier;
        public string RaidDinoCharacterFoodDrainMultiplier
        {
            get { return _raidDinoCharacterFoodDrainMultiplier; }
            set
            {
                _raidDinoCharacterFoodDrainMultiplier = value;
                OnPropertyChanged(nameof(RaidDinoCharacterFoodDrainMultiplier)); // Notify the UI of the change
            }
        }

        private string _itemStackSizeMultiplier;
        public string ItemStackSizeMultiplier
        {
            get { return _itemStackSizeMultiplier; }
            set
            {
                _itemStackSizeMultiplier = value;
                OnPropertyChanged(nameof(ItemStackSizeMultiplier)); // Notify the UI of the change
            }
        }

        private bool _allowHitMarkers;
        public bool AllowHitMarkers
        {
            get { return _allowHitMarkers; }
            set
            {
                _allowHitMarkers = value;
                OnPropertyChanged(nameof(AllowHitMarkers)); // Notify the UI of the change
            }
        }

        private bool _allowMultipleAttachedC4;
        public bool AllowMultipleAttachedC4
        {
            get { return _allowMultipleAttachedC4; }
            set
            {
                _allowMultipleAttachedC4 = value;
                OnPropertyChanged(nameof(AllowMultipleAttachedC4)); // Notify the UI of the change
            }
        }

       









    }

}