using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace WahJumps.Configuration
{
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool EnableLogging { get; set; } = false;

        public string LastSelectedView { get; set; } = "all";
        public bool SidebarHidden { get; set; } = false;

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface?.SavePluginConfig(this);
        }
    }

    public class SettingsManager
    {
        private readonly PluginConfiguration configuration;

        public SettingsManager(IDalamudPluginInterface pluginInterface)
        {
            configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            configuration.Initialize(pluginInterface);
        }

        public PluginConfiguration Configuration => configuration;

        public void SaveConfiguration() => configuration.Save();
    }
}
