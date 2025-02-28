// File: WahJumps/Windows/SettingsTab.cs
// Status: COMPLETE - Fixed tabs

using ImGuiNET;
using System;
using System.Numerics;
using WahJumps.Configuration;
using WahJumps.Logging;
using WahJumps.Utilities;
using System.IO;

namespace WahJumps.Windows
{
    public class SettingsTab
    {
        private readonly SettingsManager settingsManager;
        private readonly Action onSettingsChanged;

        private bool clearCacheConfirmation = false;
        private readonly string configDirectory;

        public SettingsTab(SettingsManager settingsManager, string configDirectory, Action onSettingsChanged)
        {
            this.settingsManager = settingsManager;
            this.configDirectory = configDirectory;
            this.onSettingsChanged = onSettingsChanged;
        }

        public void Draw()
        {
            // Use ImRaii.TabItem with no parameters - no flags needed now
            using var tabItem = new ImRaii.TabItem("Settings");
            if (!tabItem.Success) return;

            var config = settingsManager.Configuration;
            bool configChanged = false;

            using var contentChild = new ImRaii.Child("SettingsScroll", new Vector2(0, 0), true);

            // UI Settings Section
            using (var header = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
            {
                ImGui.Text("UI Settings");
            }
            ImGui.Separator();

            // Data Center Color Settings
            bool showDataCenterColors = config.ShowDataCenterColors;
            if (ImGui.Checkbox("Show Data Center Colors", ref showDataCenterColors))
            {
                config.ShowDataCenterColors = showDataCenterColors;
                configChanged = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Color-code data center tabs by region (NA, EU, JP, etc.)");
            }

            // Default View Mode
            int defaultViewMode = config.DefaultViewMode;
            string[] viewModes = new[] { "Tab View", "Unified Search" };
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Default View Mode", ref defaultViewMode, viewModes, viewModes.Length))
            {
                config.DefaultViewMode = defaultViewMode;
                configChanged = true;
            }

            // Remember Window Size
            bool rememberWindowSize = config.RememberWindowSize;
            if (ImGui.Checkbox("Remember Window Size", ref rememberWindowSize))
            {
                config.RememberWindowSize = rememberWindowSize;
                configChanged = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Save and restore window size between sessions");
            }

            // Reset UI to defaults button
            if (ImGui.Button("Reset UI to Defaults"))
            {
                config.ShowDataCenterColors = true;
                config.DefaultViewMode = 0;
                config.DefaultTab = 0;
                config.WindowSizeX = 1200;
                config.WindowSizeY = 900;
                config.RememberWindowSize = true;
                configChanged = true;
            }

            ImGui.Spacing();
            ImGui.Spacing();

            // Feature Settings Section
            using (var header = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
            {
                ImGui.Text("Feature Settings");
            }
            ImGui.Separator();

            // Logging
            bool enableLogging = config.EnableLogging;
            if (ImGui.Checkbox("Enable Debug Logging", ref enableLogging))
            {
                config.EnableLogging = enableLogging;
                CustomLogger.IsLoggingEnabled = enableLogging;
                configChanged = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Write debug information to the plugin log file");
            }

            // Auto Refresh on Startup
            bool autoRefreshOnStartup = config.AutoRefreshOnStartup;
            if (ImGui.Checkbox("Auto-refresh Data on Startup", ref autoRefreshOnStartup))
            {
                config.AutoRefreshOnStartup = autoRefreshOnStartup;
                configChanged = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Automatically refresh puzzle data when the plugin starts");
            }

            // Refresh Interval
            int refreshInterval = config.RefreshIntervalDays;
            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("Auto-refresh Interval (days)", ref refreshInterval))
            {
                // Clamp to reasonable values
                if (refreshInterval < 1) refreshInterval = 1;
                if (refreshInterval > 30) refreshInterval = 30;

                config.RefreshIntervalDays = refreshInterval;
                configChanged = true;
            }

            // Travel Confirmation
            bool showTravelConfirmation = config.ShowTravelConfirmation;
            if (ImGui.Checkbox("Show Travel Confirmation", ref showTravelConfirmation))
            {
                config.ShowTravelConfirmation = showTravelConfirmation;
                configChanged = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Show a confirmation dialog before traveling to a location");
            }

            // Speedrun Settings
            ImGui.Spacing();
            ImGui.Spacing();

            using (var header = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
            {
                ImGui.Text("Speedrun Settings");
            }
            ImGui.Separator();

            bool showSpeedrunOptions = config.ShowSpeedrunOptions;
            if (ImGui.Checkbox("Show Speedrun Options", ref showSpeedrunOptions))
            {
                config.ShowSpeedrunOptions = showSpeedrunOptions;
                configChanged = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Display speedrun related options in the interface");
            }

            // Data Cache Section
            ImGui.Spacing();
            ImGui.Spacing();

            using (var header = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
            {
                ImGui.Text("Data Cache");
            }
            ImGui.Separator();

            long cacheSize = GetCacheSize();
            ImGui.Text($"Cache Size: {FormatFileSize(cacheSize)}");

            // Clear Cache Button
            if (!clearCacheConfirmation)
            {
                if (ImGui.Button("Clear Cache"))
                {
                    clearCacheConfirmation = true;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Delete all cached puzzle data and favorites");
                }
            }
            else
            {
                using var colors = new ImRaii.StyleColor(
                    (ImGuiCol.Button, UiTheme.Error),
                    (ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.3f, 0.3f, 1.0f))
                );

                if (ImGui.Button("Confirm Clear Cache"))
                {
                    ClearCache();
                    clearCacheConfirmation = false;
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    clearCacheConfirmation = false;
                }
            }

            // About Section
            ImGui.Spacing();
            ImGui.Spacing();

            using (var header = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
            {
                ImGui.Text("About WahJumps");
            }
            ImGui.Separator();

            ImGui.TextWrapped("WahJumps is a plugin for browsing and traveling to FFXIV player-created housing jump puzzles.");
            ImGui.Spacing();
            ImGui.TextWrapped("Data is sourced from the Strange Housing community spreadsheet.");
            ImGui.Spacing();

            if (UiTheme.Hyperlink("View on GitHub"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/yourname/wahjumps",
                    UseShellExecute = true
                });
            }

            ImGui.Spacing();
            ImGui.Text("Version: 1.0.0");

            // Save configuration if changed
            if (configChanged)
            {
                settingsManager.SaveConfiguration();
                onSettingsChanged?.Invoke();
            }
        }

        private long GetCacheSize()
        {
            long size = 0;

            try
            {
                string[] files = Directory.GetFiles(configDirectory, "*.*");

                foreach (string file in files)
                {
                    var fileInfo = new FileInfo(file);
                    size += fileInfo.Length;
                }
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error calculating cache size: {ex.Message}");
            }

            return size;
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }

        private void ClearCache()
        {
            try
            {
                // Delete all CSV files
                string[] csvFiles = Directory.GetFiles(configDirectory, "*.csv");
                foreach (string file in csvFiles)
                {
                    File.Delete(file);
                }

                // Delete favorites.json
                string favoritesPath = Path.Combine(configDirectory, "favorites.json");
                if (File.Exists(favoritesPath))
                {
                    File.Delete(favoritesPath);
                }

                // Delete log file
                string logPath = Path.Combine(configDirectory, "plugin.log");
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }

                Plugin.PluginLog.Information("Cache cleared successfully");
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Error($"Error clearing cache: {ex.Message}");
            }
        }
    }
}
