using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace YourNamespace.Helpers
{
    public static class JsonHelper
    {
        private static readonly Mutex fileMutex = new Mutex(false, "Global\\ArkAscendedManagerJsonMutex");

        public static T ReadJsonFile<T>(string jsonFilePath) where T : new()
        {
            try
            {
                fileMutex.WaitOne();

                if (!File.Exists(jsonFilePath))
                {
                    Ark_Ascended_Manager.Services.Logger.Log($"File not found: {jsonFilePath}");
                    return new T();
                }

                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        using (var fs = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs))
                        {
                            string json = sr.ReadToEnd();
                            return JsonConvert.DeserializeObject<T>(json) ?? new T();
                        }
                    }
                    catch (IOException ex)
                    {
                        retries--;
                        Ark_Ascended_Manager.Services.Logger.Log($"IOException while reading file: {ex.Message}");
                        if (retries == 0)
                        {
                            Ark_Ascended_Manager.Services.Logger.Log($"Failed to read JSON after multiple attempts: {jsonFilePath}");
                            throw;
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception occurred while reading JSON file: {ex.Message}");
            }
            finally
            {
                fileMutex.ReleaseMutex();
            }
            return new T();
        }

        public static void WriteJsonFile<T>(string jsonFilePath, T content)
        {
            try
            {
                fileMutex.WaitOne();

                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        using (var fs = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (var sw = new StreamWriter(fs))
                        {
                            string json = JsonConvert.SerializeObject(content, Formatting.Indented);
                            sw.Write(json);
                            return;
                        }
                    }
                    catch (IOException ex)
                    {
                        retries--;
                        Ark_Ascended_Manager.Services.Logger.Log($"IOException while writing to file: {ex.Message}");
                        if (retries == 0)
                        {
                            Ark_Ascended_Manager.Services.Logger.Log($"Failed to write JSON after multiple attempts: {jsonFilePath}");
                            throw;
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Ark_Ascended_Manager.Services.Logger.Log($"Exception occurred while writing JSON file: {ex.Message}");
            }
            finally
            {
                fileMutex.ReleaseMutex();
            }
        }
    }
}
