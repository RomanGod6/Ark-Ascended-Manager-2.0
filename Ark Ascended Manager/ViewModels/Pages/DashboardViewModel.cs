using Ark_Ascended_Manager.Models; // Make sure to use the correct namespace where ServerProfile is defined
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

public class DashboardViewModel
{
    public ObservableCollection<ServerProfile> ServerProfiles { get; set; } // Use ServerProfile type

    public DashboardViewModel()
    {
      
    }

    

}
