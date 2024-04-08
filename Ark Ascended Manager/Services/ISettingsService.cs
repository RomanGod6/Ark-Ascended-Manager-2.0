using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Appearance;

namespace Ark_Ascended_Manager.Services
{
    internal interface ISettingsService
    {
        void LoadSettings();
        ThemeType CurrentTheme { get; set; }
    }
}
