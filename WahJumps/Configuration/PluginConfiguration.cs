// File: WahJumps/Configuration/PluginConfiguration.cs
// Status: UPDATED - Removed compact mode option

using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.IO;
using Newtonsoft.Json;

namespace WahJumps.Configuration
{
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        // UI Settings
        public bool ShowDataCenterColors { get; set; } = true;
        public int DefaultViewMode { get; set; } = 0; // 0=Tabs, 1=Unified Search
        public int DefaultTab { get; set; } = 0;

        // Feature Settings
        public bool EnableLogging { get; set; } = false;
        public bool AutoRefreshOnStartup { get; set; } = true;
        public int RefreshIntervalDays { get; set; } = 7;
        public bool ShowTravelConfirmation { get; set; } = true;

        // Filter Settings
        public string DefaultDataCenter { get; set; } = "All";
        public string DefaultWorld { get; set; } = "All";
        public string DefaultRating { get; set; } = "All";

        // Window Settings
        public float WindowSizeX { get; set; } = 1200;
        public float WindowSizeY { get; set; } = 900;
        public bool RememberWindowSize { get; set; } = true;

        // The ConfigManager service tracks whether plugin config is saved or not
        [NonSerialized]
        private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }

    public class SettingsManager
    {
        private readonly PluginConfiguration configuration;
        private readonly string configDirectory;

        public SettingsManager(IDalamudPluginInterface pluginInterface, string configDirectory)
        {
            this.configDirectory = configDirectory;

            // Load or create configuration
            configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            configuration.Initialize(pluginInterface);
        }

        public PluginConfiguration Configuration => configuration;

        public void SaveConfiguration()
        {
            configuration.Save();
        }

        public T LoadJsonFile<T>(string filename, T defaultValue = default)
        {
            string filePath = Path.Combine(configDirectory, filename);

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error loading {filename}: {ex.Message}");
            }

            return defaultValue;
        }

        public void SaveJsonFile<T>(string filename, T data)
        {
            string filePath = Path.Combine(configDirectory, filename);

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error saving {filename}: {ex.Message}");
            }
        }
    }
}
