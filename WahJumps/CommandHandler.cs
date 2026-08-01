using System;
using Dalamud.Plugin.Services;
using WahJumps.Logging;
using WahJumps.Windows;

namespace WahJumps
{
    public class CommandHandler
    {
        private readonly IChatGui chatGui;
        private readonly MainWindow mainWindow;

        public CommandHandler(IChatGui chatGui, MainWindow mainWindow)
        {
            this.chatGui = chatGui;
            this.mainWindow = mainWindow;
        }

        public void HandleCommand(string command, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                mainWindow.ToggleVisibility();
                return;
            }

            string[] argParts = args.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string mainArg = argParts[0];

            switch (mainArg)
            {
                case "debug":
                    HandleDebugCommand();
                    break;
                default:
                    mainWindow.ToggleVisibility();
                    break;
            }
        }

        private void HandleDebugCommand()
        {
            var config = mainWindow.GetConfiguration();
            config.EnableLogging = !config.EnableLogging;
            CustomLogger.IsLoggingEnabled = config.EnableLogging;
            config.Save();

            string status = config.EnableLogging ? "enabled" : "disabled";
            chatGui.Print($"[WahJumps] Debug logging {status}");
        }
    }
}
