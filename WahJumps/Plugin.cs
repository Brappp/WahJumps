using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.IoC;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using WahJumps.Handlers;
using WahJumps.Windows;
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
        public MainWindow MainWindow { get; private set; }

        public readonly WindowSystem WindowSystem = new("WahJumps");
        private readonly CommandHandler commandHandler;
        private string ConfigDirectory { get; }

        public Plugin()
        {
            ConfigDirectory = CreateConfigDirectory();

            LifestreamIpcHandler = new LifestreamIpcHandler(PluginInterface);
            CsvManager = new CsvManager(ChatGui, ConfigDirectory);

            MainWindow = new MainWindow(CsvManager, LifestreamIpcHandler);

            WindowSystem.AddWindow(MainWindow);

            commandHandler = new CommandHandler(ChatGui, MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the WahJumps UI."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleVisibility;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            PluginLog.Information("WahJumps plugin initialized");
            CustomLogger.Log("Plugin initialized successfully");
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

            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleVisibility;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            CsvManager.Dispose();
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            commandHandler.HandleCommand(command, args);
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void ToggleVisibility() => MainWindow.ToggleVisibility();
    }
}
