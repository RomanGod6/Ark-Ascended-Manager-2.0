using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Ark_Ascended_Manager.Models;

namespace Ark_Ascended_Manager.Services
{
    public class MapService
    {
        private static MapService _instance;
        private List<MapInfo> _maps;
        private readonly string _mapsConfigPath;

        public static MapService Instance => _instance ??= new MapService();

        private MapService()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string aamFolder = Path.Combine(appDataFolder, "Ark Ascended Manager");
            Directory.CreateDirectory(aamFolder);
            _mapsConfigPath = Path.Combine(aamFolder, "maps.json");

            LoadMaps();
        }

        public List<MapInfo> GetAllMaps()
        {
            return _maps?.ToList() ?? new List<MapInfo>();
        }

        public Dictionary<string, string> GetMapDictionary()
        {
            return _maps?.ToDictionary(m => m.MapCode, m => m.DisplayName) ?? new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetMapToAppIdDictionary()
        {
            return _maps?.ToDictionary(m => m.MapCode, m => m.AppId) ?? new Dictionary<string, string>();
        }

        public void AddCustomMap(MapInfo map)
        {
            if (_maps == null)
            {
                _maps = new List<MapInfo>();
            }

            // Check if map already exists
            if (_maps.Any(m => m.MapCode.Equals(map.MapCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Map with code '{map.MapCode}' already exists.");
            }

            map.IsCustom = true;
            _maps.Add(map);
            SaveMaps();
        }

        public void RemoveCustomMap(string mapCode)
        {
            var map = _maps?.FirstOrDefault(m => m.MapCode.Equals(mapCode, StringComparison.OrdinalIgnoreCase));
            if (map == null)
            {
                throw new InvalidOperationException($"Map with code '{mapCode}' not found.");
            }

            if (!map.IsCustom)
            {
                throw new InvalidOperationException("Cannot remove official maps.");
            }

            _maps.Remove(map);
            SaveMaps();
        }

        public void UpdateMap(MapInfo updatedMap)
        {
            var existingMap = _maps?.FirstOrDefault(m => m.MapCode.Equals(updatedMap.MapCode, StringComparison.OrdinalIgnoreCase));
            if (existingMap == null)
            {
                throw new InvalidOperationException($"Map with code '{updatedMap.MapCode}' not found.");
            }

            // Update properties
            existingMap.DisplayName = updatedMap.DisplayName;
            existingMap.AppId = updatedMap.AppId;
            SaveMaps();
        }

        private void LoadMaps()
        {
            try
            {
                if (File.Exists(_mapsConfigPath))
                {
                    string json = File.ReadAllText(_mapsConfigPath);
                    _maps = JsonConvert.DeserializeObject<List<MapInfo>>(json);
                    Logger.Log("Maps loaded from configuration file.");
                }
                else
                {
                    // Create default map configuration
                    CreateDefaultMapsConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading maps configuration: {ex.Message}. Using default maps.");
                CreateDefaultMapsConfig();
            }
        }

        private void CreateDefaultMapsConfig()
        {
            _maps = new List<MapInfo>
            {
                new MapInfo { MapCode = "TheIsland_WP", DisplayName = "The Island", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "ScorchedEarth_WP", DisplayName = "Scorched Earth", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "TheCenter_WP", DisplayName = "The Center", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "Aberration_WP", DisplayName = "Aberration", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "Extinction_WP", DisplayName = "Extinction", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "Ragnarok_WP", DisplayName = "Ragnarok", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "Valguero_WP", DisplayName = "Valguero", AppId = "2430930", IsOfficial = true },
                new MapInfo { MapCode = "Astraeos_WP", DisplayName = "Astraeos", AppId = "2430930", IsOfficial = true }
            };

            SaveMaps();
            Logger.Log("Default maps configuration created.");
        }

        private void SaveMaps()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_maps, Formatting.Indented);
                File.WriteAllText(_mapsConfigPath, json);
                Logger.Log("Maps configuration saved.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving maps configuration: {ex.Message}");
            }
        }

        public void ReloadMaps()
        {
            LoadMaps();
        }

        public string GetMapsConfigPath()
        {
            return _mapsConfigPath;
        }
    }
}
