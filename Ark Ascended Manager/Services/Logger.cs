using System;
using System.IO;

namespace Ark_Ascended_Manager.Services
{
    internal class Logger
    {
        private static readonly string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ark Ascended Manager", "debug.log");

        public static void Log(string message)
        {
            string logEntry = $"{DateTime.Now}: {message}\n";
            try
            {
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                // Optionally handle exceptions, such as logging to an alternative location
                // Example: Console.WriteLine($"Error logging message: {ex.Message}");
            }
        }
    }
}