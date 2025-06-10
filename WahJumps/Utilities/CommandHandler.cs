using System;
using Dalamud.Plugin.Services;
using WahJumps.Configuration;
using WahJumps.Data;
using WahJumps.Logging;
using WahJumps.Windows;

namespace WahJumps.Utilities
{
    public class CommandHandler
    {
        private readonly IChatGui chatGui;
        private readonly SpeedrunManager speedrunManager;
        private readonly TimerWindow timerWindow;
        private readonly MainWindow mainWindow;

        public CommandHandler(IChatGui chatGui, SpeedrunManager speedrunManager, 
            TimerWindow timerWindow, MainWindow mainWindow)
        {
            this.chatGui = chatGui;
            this.speedrunManager = speedrunManager;
            this.timerWindow = timerWindow;
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
                case "timer":
                    HandleTimerCommand(argParts);
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

        private void HandleTimerCommand(string[] argParts)
        {
            if (argParts.Length == 1)
            {
                timerWindow.Toggle();
                return;
            }

            string timerArg = argParts[1];
            switch (timerArg)
            {
                case "start":
                    speedrunManager.StartCountdown();
                    timerWindow.ShowTimer();
                    chatGui.Print("[WahJumps] Timer started with countdown");
                    break;
                case "stop":
                    if (speedrunManager.GetState() == SpeedrunManager.SpeedrunState.Running)
                    {
                        speedrunManager.StopTimer();
                        chatGui.Print("[WahJumps] Timer stopped");
                    }
                    else
                    {
                        chatGui.Print("[WahJumps] Timer is not currently running");
                    }
                    break;
                case "reset":
                    speedrunManager.ResetTimer();
                    chatGui.Print("[WahJumps] Timer reset");
                    break;
                case "show":
                    timerWindow.ShowTimer();
                    chatGui.Print("[WahJumps] Timer window shown");
                    break;
                case "hide":
                    timerWindow.HideTimer();
                    chatGui.Print("[WahJumps] Timer window hidden");
                    break;
                default:
                    chatGui.Print("[WahJumps] Timer commands: start, stop, reset, show, hide");
                    break;
            }
        }
    }
} 