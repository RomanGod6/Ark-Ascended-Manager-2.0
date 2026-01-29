namespace Ark_Ascended_Manager.Models
{
    public class MapInfo
    {
        public string MapCode { get; set; }       // e.g., "TheIsland_WP"
        public string DisplayName { get; set; }   // e.g., "The Island"
        public string AppId { get; set; }         // e.g., "2430930"
        public bool IsOfficial { get; set; }      // True for official maps
        public bool IsCustom { get; set; }        // True for user-added maps

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
