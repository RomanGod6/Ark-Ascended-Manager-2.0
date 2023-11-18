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
        LoadData(); // Load the data when the ViewModel is created
    }

    private void LoadData()
    {
        // Get the folder path for the current user's AppData directory
        string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Combine the AppData path with your application's specific folder
        string applicationFolderPath = Path.Combine(appDataFolderPath, "Ark Ascended Manager");

        // Ensure the directory exists
        Directory.CreateDirectory(applicationFolderPath);

        // Define the file path for servers.json within the application's folder
        string serverProfilesPath = Path.Combine(applicationFolderPath, "servers.json");

        // Load Server Profiles
        ServerProfiles = new ObservableCollection<ServerProfile>(ServerProfile.LoadAllServerProfiles(serverProfilesPath));

        // Define the file path for scheduling.json within the application's folder
        string schedulingDataPath = Path.Combine(applicationFolderPath, "allServersSchedulingData.json");

        // Load Scheduling Data and merge it with Server Profiles
        var schedulingData = ServerProfile.LoadAllSchedulingData(schedulingDataPath);
        foreach (var serverProfile in ServerProfiles)
        {
            if (schedulingData.TryGetValue(serverProfile.ProfileName, out var scheduleEntries))
            {
                serverProfile.SchedulingData = scheduleEntries;
            }
        }

    }

}
