// File: WahJumps/Windows/SettingsTab.cs
using ImGuiNET;
using System;
using System.Numerics;
using WahJumps.Configuration;
using WahJumps.Logging;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class SettingsTab
    {
        private readonly SettingsManager settingsManager;
        private readonly Action onSettingsChanged;

        public SettingsTab(SettingsManager settingsManager, string configDirectory, Action onSettingsChanged)
        {
            this.settingsManager = settingsManager;
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

            ImGui.Spacing();
            ImGui.Spacing();

            // About Section
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
                    FileName = "https://github.com/Brappp/WahJumps",
                    UseShellExecute = true
                });
            }

            ImGui.Spacing();

            // Save configuration if changed
            if (configChanged)
            {
                settingsManager.SaveConfiguration();
                onSettingsChanged?.Invoke();
            }
        }
    }
}
