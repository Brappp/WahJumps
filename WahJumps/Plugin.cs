// File: WahJumps/Plugin.cs
// Status: ENHANCED VERSION - Improved speedrun functionality with splits and templates

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
        private Dictionary<string, List<SplitTemplate>> defaultTemplates;

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
            InitializeDefaultSplitTemplates();

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
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Register event handlers
            SpeedrunManager.RunCompleted += OnRunCompleted;

            // Log initialization
            PluginLog.Information("WahJumps plugin initialized with enhanced speedrun functionality");
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

            // Create subdirectories for templates and custom puzzles
            string templatesDir = Path.Combine(outputDirectory, "Templates");
            if (!Directory.Exists(templatesDir))
            {
                Directory.CreateDirectory(templatesDir);
            }

            string customPuzzlesDir = Path.Combine(outputDirectory, "CustomPuzzles");
            if (!Directory.Exists(customPuzzlesDir))
            {
                Directory.CreateDirectory(customPuzzlesDir);
            }

            return outputDirectory;
        }

        private void InitializeDefaultSplitTemplates()
        {
            defaultTemplates = new Dictionary<string, List<SplitTemplate>>();

            // Basic templates with common splits
            var basicTemplate = new SplitTemplate("Basic Jump Template");
            basicTemplate.Splits.Add(new SplitCheckpoint("Start Platform", 0));
            basicTemplate.Splits.Add(new SplitCheckpoint("Halfway Point", 1));
            basicTemplate.Splits.Add(new SplitCheckpoint("Final Stretch", 2));
            basicTemplate.Splits.Add(new SplitCheckpoint("Finish", 3));

            // Add to the default templates collection
            defaultTemplates["Basic"] = new List<SplitTemplate> { basicTemplate };

            // Check if any default templates should be added to the manager
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

            // Unregister event handlers
            SpeedrunManager.RunCompleted -= OnRunCompleted;

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            SpeedrunOverlayWindow.Dispose();
            SpeedrunRecordsWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(TimerCommandName);
            CommandManager.RemoveHandler(SplitsCommandName);
            CommandManager.RemoveHandler(RecordsCommandName);
        }

        private void OnCommand(string command, string args) => ToggleMainUI();

        private void OnTimerCommand(string command, string args)
        {
            // Parse arguments to provide more functionality
            string[] argParts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (argParts.Length == 0)
            {
                // Default behavior - show the timer overlay
                ToggleSpeedrunOverlay();
                return;
            }

            string subCommand = argParts[0].ToLower();

            switch (subCommand)
            {
                case "records":
                case "history":
                    ToggleSpeedrunRecords();
                    break;

                case "start":
                    // Start the timer with a specific puzzle (if selected in main window)
                    if (SpeedrunManager.GetCurrentPuzzle() != null)
                    {
                        ToggleSpeedrunOverlay();
                        SpeedrunManager.StartCountdown();
                    }
                    else
                    {
                        ChatGui.Print("[WahJumps] No puzzle selected. Please select a puzzle first.");
                    }
                    break;

                case "split":
                    // Mark a split if the timer is running
                    if (SpeedrunManager.GetState() == SpeedrunManager.SpeedrunState.Running)
                    {
                        SpeedrunManager.MarkSplit();
                        ChatGui.Print("[WahJumps] Split marked!");
                    }
                    else
                    {
                        ChatGui.Print("[WahJumps] Timer not running. Cannot mark split.");
                    }
                    break;

                case "stop":
                    // Stop the timer if it's running
                    if (SpeedrunManager.GetState() == SpeedrunManager.SpeedrunState.Running)
                    {
                        SpeedrunManager.StopTimer();
                        ChatGui.Print("[WahJumps] Timer stopped!");
                    }
                    else
                    {
                        ChatGui.Print("[WahJumps] Timer not running.");
                    }
                    break;

                case "reset":
                    // Reset the timer
                    SpeedrunManager.ResetTimer();
                    ChatGui.Print("[WahJumps] Timer reset!");
                    break;

                case "help":
                    // Show help message
                    ChatGui.Print("[WahJumps] Speedrun Timer Commands:");
                    ChatGui.Print("  /jumptimer          - Toggle timer overlay");
                    ChatGui.Print("  /jumptimer records  - Show records window");
                    ChatGui.Print("  /jumptimer start    - Start timer with selected puzzle");
                    ChatGui.Print("  /jumptimer split    - Mark a split during a run");
                    ChatGui.Print("  /jumptimer stop     - Stop the timer");
                    ChatGui.Print("  /jumptimer reset    - Reset the timer");
                    break;

                default:
                    // Show the timer overlay for any other input
                    ToggleSpeedrunOverlay();
                    break;
            }
        }

        private void OnSplitsCommand(string command, string args)
        {
            // Toggle the speedrun overlay window
            ToggleSpeedrunOverlay();
        }

        private void OnRecordsCommand(string command, string args)
        {
            // Toggle the speedrun records window
            ToggleSpeedrunRecords();
        }

        private void DrawUI() => WindowSystem.Draw();

        public void ToggleMainUI() => MainWindow.IsOpen = !MainWindow.IsOpen;

        public void ToggleSpeedrunOverlay() => SpeedrunOverlayWindow.IsOpen = !SpeedrunOverlayWindow.IsOpen;

        public void ToggleSpeedrunRecords() => SpeedrunRecordsWindow.IsOpen = !SpeedrunRecordsWindow.IsOpen;

        public void SelectPuzzleForSpeedrun(JumpPuzzleData puzzle)
        {
            if (puzzle != null)
            {
                // Set the puzzle in the speedrun manager
                SpeedrunManager.SetPuzzle(puzzle);

                // Open the speedrun overlay
                ToggleSpeedrunOverlay();
            }
        }

        // Method to get default templates for a specific category
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
