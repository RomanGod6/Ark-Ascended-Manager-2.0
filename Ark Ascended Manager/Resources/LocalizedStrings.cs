using System;
using System.Globalization;
using System.Resources;

namespace Ark_Ascended_Manager.Resources
{
    public class LocalizedStrings
    {
        private static ResourceManager _resourceManager = new ResourceManager("Ark_Ascended_Manager.Resources.Resources", typeof(LocalizedStrings).Assembly);

        public string AdminWarning => _resourceManager.GetString("AdminWarning", CultureInfo.CurrentCulture);
        public string ComingSoon => _resourceManager.GetString("ComingSoon", CultureInfo.CurrentCulture);
        public string RunAsAdmin => _resourceManager.GetString("RunAsAdmin", CultureInfo.CurrentCulture);
    }
}
