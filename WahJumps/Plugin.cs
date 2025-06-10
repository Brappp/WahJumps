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

        public CsvManager CsvManager { get; private set; }
        public LifestreamIpcHandler LifestreamIpcHandler { get; private set; }

        // Speedrun components
        public SpeedrunManager SpeedrunManager { get; private set; }
        public TimerWindow TimerWindow { get; private set; }
        public MainWindow MainWindow { get; private set; }

        public readonly WindowSystem WindowSystem = new("WahJumps");
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

            // Add windows to the window system
            WindowSystem.AddWindow(MainWindow);

            // Create and add the timer window
            TimerWindow = new TimerWindow(SpeedrunManager, this);
            WindowSystem.AddWindow(TimerWindow);

            // Register commands
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the WahJumps UI."
            });

            // Register UI events
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleVisibility;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Register event handlers
            SpeedrunManager.StateChanged += OnStateChanged;

            // Log initialization
            PluginLog.Information("WahJumps plugin initialized with simplified timer functionality");

            // Initialize logging
            CustomLogger.Log("Plugin initialized successfully");
        }

        private void OnStateChanged(SpeedrunManager.SpeedrunState state)
        {
            // Notify the user via chat when a run is completed
            if (state == SpeedrunManager.SpeedrunState.Finished)
            {
                var time = SpeedrunManager.GetCurrentTime();
                string timeText = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";

                var puzzleName = SpeedrunManager.GetCurrentPuzzle()?.PuzzleName ?? "current puzzle";
                ChatGui.Print($"[WahJumps] Run for {puzzleName} completed! Time: {timeText}");
            }
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

            // Unsubscribe from events
            SpeedrunManager.StateChanged -= OnStateChanged;

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            TimerWindow.Dispose();

            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // Handle subcommands
            if (!string.IsNullOrEmpty(args))
            {
                string[] argParts = args.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string mainArg = argParts[0];
                
                if (mainArg == "debug")
                {
                    // Toggle debug logging
                    var config = MainWindow.GetConfiguration();
                    config.EnableLogging = !config.EnableLogging;
                    CustomLogger.IsLoggingEnabled = config.EnableLogging;
                    config.Save();
                    
                    string status = config.EnableLogging ? "enabled" : "disabled";
                    ChatGui.Print($"[WahJumps] Debug logging {status}");
                    return;
                }
                else if (mainArg == "timer")
                {
                    // Handle timer commands
                    if (argParts.Length == 1)
                    {
                        // Just "timer" - toggle timer window
                        TimerWindow.Toggle();
                        return;
                    }
                    
                    string timerArg = argParts[1];
                    switch (timerArg)
                    {
                        case "start":
                            SpeedrunManager.StartCountdown();
                            TimerWindow.ShowTimer();
                            ChatGui.Print("[WahJumps] Timer started with countdown");
                            break;
                        case "stop":
                            if (SpeedrunManager.GetState() == SpeedrunManager.SpeedrunState.Running)
                            {
                                SpeedrunManager.StopTimer();
                                ChatGui.Print("[WahJumps] Timer stopped");
                            }
                            else
                            {
                                ChatGui.Print("[WahJumps] Timer is not currently running");
                            }
                            break;
                        case "reset":
                            SpeedrunManager.ResetTimer();
                            ChatGui.Print("[WahJumps] Timer reset");
                            break;
                        case "show":
                            TimerWindow.ShowTimer();
                            ChatGui.Print("[WahJumps] Timer window shown");
                            break;
                        case "hide":
                            TimerWindow.HideTimer();
                            ChatGui.Print("[WahJumps] Timer window hidden");
                            break;
                        default:
                            ChatGui.Print("[WahJumps] Timer commands: start, stop, reset, show, hide");
                            break;
                    }
                    return;
                }
                else
                {
                    // Unknown subcommand - just open main window
                    MainWindow.ToggleVisibility();
                    return;
                }
            }
            
            // Default behavior - toggle main window
            MainWindow.ToggleVisibility();
        }

        private void DrawUI()
        {
            // Update the speedrun timer every frame
            SpeedrunManager.Update();

            WindowSystem.Draw();
        }

        public void ToggleVisibility() => MainWindow.ToggleVisibility();

        public void ToggleSpeedrunOverlay() => TimerWindow.Toggle();

        public void SelectPuzzleForSpeedrun(JumpPuzzleData puzzle)
        {
            if (puzzle != null)
            {
                SpeedrunManager.SetPuzzle(puzzle);
                TimerWindow.ShowTimer();
            }
        }
    }
}
