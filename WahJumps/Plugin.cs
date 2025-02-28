// File: WahJumps/Plugin.cs
// Status: FIXED VERSION - Added speedrun functionality

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using WahJumps.Handlers;
using WahJumps.Windows;
using WahJumps.Data;
using System;

namespace WahJumps
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

        private const string CommandName = "/WahJumps";
        private const string TimerCommandName = "/JumpTimer";

        public CsvManager CsvManager { get; private set; }
        public LifestreamIpcHandler LifestreamIpcHandler { get; private set; }

        // Speedrun components
        public SpeedrunManager SpeedrunManager { get; private set; }

        public readonly WindowSystem WindowSystem = new("WahJumps");
        private MainWindow MainWindow { get; init; }
        private SpeedrunOverlayWindow SpeedrunOverlayWindow { get; init; }
        private SpeedrunRecordsWindow SpeedrunRecordsWindow { get; init; }
        private string ConfigDirectory { get; }

        public Plugin()
        {
            // Set up configuration directory
            ConfigDirectory = CreateConfigDirectory();

            // Initialize handlers
            LifestreamIpcHandler = new LifestreamIpcHandler(PluginInterface);
            CsvManager = new CsvManager(ChatGui, ConfigDirectory);

            // Initialize speedrun components
            SpeedrunManager = new SpeedrunManager(ConfigDirectory);

            // Set up windows
            MainWindow = new MainWindow(CsvManager, LifestreamIpcHandler, this);
            SpeedrunOverlayWindow = new SpeedrunOverlayWindow(SpeedrunManager);
            SpeedrunRecordsWindow = new SpeedrunRecordsWindow(SpeedrunManager);

            // Add windows to the window system
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(SpeedrunOverlayWindow);
            WindowSystem.AddWindow(SpeedrunRecordsWindow);

            // Register commands
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the WahJumps UI for finding jump puzzles"
            });

            CommandManager.AddHandler(TimerCommandName, new CommandInfo(OnTimerCommand)
            {
                HelpMessage = "Opens the jump puzzle speedrun timer"
            });

            // Register UI events
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Log initialization
            PluginLog.Information("WahJumps plugin initialized");
        }

        private string CreateConfigDirectory()
        {
            string outputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "pluginConfigs",
                "WahJumps"
            );

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                PluginLog.Information($"Created configuration directory: {outputDirectory}");
            }

            return outputDirectory;
        }

        private void ToggleConfigUI() => MainWindow.ToggleVisibility();

        public void Dispose()
        {
            PluginLog.Information("Disposing WahJumps plugin");

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            SpeedrunOverlayWindow.Dispose();
            SpeedrunRecordsWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(TimerCommandName);
        }

        private void OnCommand(string command, string args) => ToggleMainUI();

        private void OnTimerCommand(string command, string args)
        {
            // Show speedrun windows based on arguments
            if (args.ToLower() == "records" || args.ToLower() == "history")
            {
                ToggleSpeedrunRecords();
            }
            else
            {
                // Default to the timer overlay
                ToggleSpeedrunOverlay();
            }
        }

        private void DrawUI() => WindowSystem.Draw();

        public void ToggleMainUI() => MainWindow.IsOpen = !MainWindow.IsOpen;

        public void ToggleSpeedrunOverlay() => SpeedrunOverlayWindow.IsOpen = !SpeedrunOverlayWindow.IsOpen;

        public void ToggleSpeedrunRecords() => SpeedrunRecordsWindow.IsOpen = !SpeedrunRecordsWindow.IsOpen;
    }
}
