using System.Security.Principal;

namespace Ark_Ascended_Manager.Resources
{
    public class AppAdminChecker
    {
        public static bool IsRunningAsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
