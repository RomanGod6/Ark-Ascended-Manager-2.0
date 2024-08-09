using System.Windows.Controls;
using System.Windows.Navigation;

namespace Ark_Ascended_Manager.Views.Pages
{
    public partial class ArkPluginResource : Page
    {
        public ArkPluginResource(Resource resource)
        {
            InitializeComponent();
            DataContext = resource;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
