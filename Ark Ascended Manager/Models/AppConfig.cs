// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace Ark_Ascended_Manager.Models
{
    public class AppConfig
    {

        public string ConfigurationsFolder { get; set; }

        public string AppPropertiesFileName { get; set; }

        // Custom maps added by users
        public List<CustomMap> CustomMaps { get; set; } = new List<CustomMap>();
    }

    public class CustomMap
    {
        public string MapCode { get; set; }  // e.g., "MyCustomMap_WP"
        public string DisplayName { get; set; } // e.g., "My Custom Map"
        public string AppId { get; set; } = "2430930"; // Default ASA App ID
    }
}
