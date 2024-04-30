using System.Windows;
using Ark_Ascended_Manager.Resources;

public class DashboardViewModel
{
    public Visibility AdminWarningVisibility { get; private set; }
    public Visibility AdminButtonVisibility { get; private set; }

    public DashboardViewModel()
    {
        bool isAdmin = AppAdminChecker.IsRunningAsAdministrator();
        AdminWarningVisibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
        AdminButtonVisibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
    }
}
