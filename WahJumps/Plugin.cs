using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using WahJumps.Handlers;
using WahJumps.Windows;
using WahJumps.Data;
using WahJumps.Utilities;
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
        public SpeedrunManager SpeedrunManager { get; private set; }
        public TimerWindow TimerWindow { get; private set; }
        public MainWindow MainWindow { get; private set; }

        public readonly WindowSystem WindowSystem = new("WahJumps");
        private readonly CommandHandler commandHandler;
        private string ConfigDirectory { get; }

        public Plugin()
        {
            ConfigDirectory = CreateConfigDirectory();

            LifestreamIpcHandler = new LifestreamIpcHandler(PluginInterface);
            CsvManager = new CsvManager(ChatGui, ConfigDirectory);
            SpeedrunManager = new SpeedrunManager(ConfigDirectory);

            MainWindow = new MainWindow(CsvManager, LifestreamIpcHandler, this);
            TimerWindow = new TimerWindow(SpeedrunManager, this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(TimerWindow);

            commandHandler = new CommandHandler(ChatGui, SpeedrunManager, TimerWindow, MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the WahJumps UI."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleVisibility;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            SpeedrunManager.StateChanged += OnStateChanged;

            PluginLog.Information("WahJumps plugin initialized");
            CustomLogger.Log("Plugin initialized successfully");
        }

        private void OnStateChanged(SpeedrunManager.SpeedrunState state)
        {
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

            SpeedrunManager.StateChanged -= OnStateChanged;
            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            TimerWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            commandHandler.HandleCommand(command, args);
        }

        private void DrawUI()
        {
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
