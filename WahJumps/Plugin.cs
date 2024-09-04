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
using System.Threading.Tasks;

namespace WahJumps
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

        private const string CommandName = "/WahJumps";

        public CsvManager CsvManager { get; private set; }
        public LifestreamIpcHandler LifestreamIpcHandler { get; private set; }

        public readonly WindowSystem WindowSystem = new("WahJumps");
        private MainWindow MainWindow { get; init; }

        public Plugin()
        {
            string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "pluginConfigs", "WahJumps");

            CustomLogger.ClearLog();

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            CsvManager = new CsvManager(ChatGui, outputDirectory);
            CsvManager.StatusUpdated += OnCsvStatusUpdated; 
            MainWindow = new MainWindow(CsvManager);

            // Trigger the download process
            Task.Run(async () => await CsvManager.DownloadAndSaveIndividualCsvsAsync());

            WindowSystem.AddWindow(MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the WahJumps UI"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        }

        private void OnCsvStatusUpdated(string message)
        {
            CustomLogger.Log(message); 
        }

        private void ToggleConfigUI()
        {
            CustomLogger.Log("Config UI toggled");
            MainWindow.Toggle();
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            CustomLogger.Log($"Command received: {command}");
            ToggleMainUI();
        }

        private void DrawUI()
        {
            CustomLogger.Log("Drawing UI");
            WindowSystem.Draw();
        }

        public void ToggleMainUI()
        {
            CustomLogger.Log("Main window toggled");
            MainWindow.Toggle();
        }

    }
}
