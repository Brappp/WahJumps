// File: WahJumps/Plugin.cs
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
using System.Linq;
using System.Collections.Generic;
using WahJumps.Logging;

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
        private const string SplitsCommandName = "/JumpSplits";
        private const string RecordsCommandName = "/JumpRecords";

        public CsvManager CsvManager { get; private set; }
        public LifestreamIpcHandler LifestreamIpcHandler { get; private set; }

        // Speedrun components
        public SpeedrunManager SpeedrunManager { get; private set; }
        public SpeedrunTab SpeedrunTab { get; private set; }
        public TimerWindow TimerWindow { get; private set; }
        private Dictionary<string, List<SplitTemplate>> defaultTemplates;

        public readonly WindowSystem WindowSystem = new("WahJumps");
        private MainWindow MainWindow { get; init; }
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
            InitializeDefaultSplitTemplates();

            // Set up windows
            MainWindow = new MainWindow(CsvManager, LifestreamIpcHandler, this);

            // Initialize SpeedrunTab
            SpeedrunTab = new SpeedrunTab(SpeedrunManager, this);

            // Add windows to the window system
            WindowSystem.AddWindow(MainWindow);

            // Create and add the timer window
            TimerWindow = new TimerWindow(SpeedrunManager);
            WindowSystem.AddWindow(TimerWindow);

            // Register commands
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the WahJumps UI for finding jump puzzles"
            });

            CommandManager.AddHandler(TimerCommandName, new CommandInfo(OnTimerCommand)
            {
                HelpMessage = "Opens the jump puzzle speedrun timer"
            });

            CommandManager.AddHandler(SplitsCommandName, new CommandInfo(OnSplitsCommand)
            {
                HelpMessage = "Toggle speedrun splits timer overlay"
            });

            CommandManager.AddHandler(RecordsCommandName, new CommandInfo(OnRecordsCommand)
            {
                HelpMessage = "Show speedrun records window"
            });

            // Register UI events
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleVisibility;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Register event handlers
            SpeedrunManager.RunCompleted += OnRunCompleted;

            // Log initialization
            PluginLog.Information("WahJumps plugin initialized with enhanced speedrun functionality");

            // Initialize logging
            CustomLogger.Log("Plugin initialized successfully");
        }

        private void OnRunCompleted(SpeedrunRecord record)
        {
            // Notify the user via chat when a run is completed
            string timeText = $"{(int)record.Time.TotalMinutes:D2}:{record.Time.Seconds:D2}.{record.Time.Milliseconds / 10:D2}";
            ChatGui.Print($"[WahJumps] Speedrun for {record.PuzzleName} completed! Time: {timeText}");
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

        private void InitializeDefaultSplitTemplates()
        {
            defaultTemplates = new Dictionary<string, List<SplitTemplate>>();

            var basicTemplate = new SplitTemplate("Basic Jump Template");
            basicTemplate.Splits.Add(new SplitCheckpoint("Start Platform", 0));
            basicTemplate.Splits.Add(new SplitCheckpoint("Halfway Point", 1));
            basicTemplate.Splits.Add(new SplitCheckpoint("Final Stretch", 2));
            basicTemplate.Splits.Add(new SplitCheckpoint("Finish", 3));

            defaultTemplates["Basic"] = new List<SplitTemplate> { basicTemplate };

            var existingTemplates = SpeedrunManager.GetTemplates();
            if (!existingTemplates.Any(t => t.Name == "Basic Jump Template"))
            {
                SpeedrunManager.UpdateTemplate(basicTemplate);
            }
        }

        private void ToggleConfigUI() => MainWindow.ToggleVisibility();

        public void Dispose()
        {
            PluginLog.Information("Disposing WahJumps plugin");

            // Unsubscribe from events
            SpeedrunManager.RunCompleted -= OnRunCompleted;

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            SpeedrunTab.Dispose();
            TimerWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(TimerCommandName);
            CommandManager.RemoveHandler(SplitsCommandName);
            CommandManager.RemoveHandler(RecordsCommandName);
        }

        private void OnCommand(string command, string args) => MainWindow.ToggleVisibility();

        private void OnTimerCommand(string command, string args)
        {
            // Toggle the timer window
            TimerWindow.Toggle();
        }

        private void OnSplitsCommand(string command, string args)
        {
            MainWindow.ToggleVisibility();
            SpeedrunTab.ForceActivate();
        }

        private void OnRecordsCommand(string command, string args)
        {
            MainWindow.ToggleVisibility();
            SpeedrunTab.ForceActivate();
        }

        private void DrawUI()
        {
            // Update the speedrun timer every frame
            SpeedrunManager.Update();

            WindowSystem.Draw();
        }

        public void ToggleVisibility() => MainWindow.ToggleVisibility();

        public void ToggleSpeedrunOverlay() => TimerWindow.Toggle();

        public void ToggleSpeedrunRecords() => MainWindow.ToggleVisibility();

        public void SelectPuzzleForSpeedrun(JumpPuzzleData puzzle)
        {
            if (puzzle != null)
            {
                SpeedrunTab.SetPuzzle(puzzle);
                TimerWindow.ShowTimer();
                MainWindow.ToggleVisibility();
            }
        }

        public List<SplitTemplate> GetDefaultTemplates(string category)
        {
            if (defaultTemplates.TryGetValue(category, out var templates))
            {
                return templates;
            }

            return new List<SplitTemplate>();
        }
    }
}
