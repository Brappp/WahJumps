using System;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace WahJumps.Handlers
{
    public class LifestreamIpcHandler
    {
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly ICallGateSubscriber<string, object> executeCommandSubscriber;

        private DateTime lastProbe = DateTime.MinValue;
        private bool lastAvailable;

        public LifestreamIpcHandler(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            executeCommandSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Lifestream.ExecuteCommand");
        }

        public bool IsAvailable
        {
            get
            {
                if ((DateTime.UtcNow - lastProbe).TotalSeconds > 5)
                {
                    lastProbe = DateTime.UtcNow;
                    lastAvailable = pluginInterface.InstalledPlugins
                        .Any(p => p.InternalName == "Lifestream" && p.IsLoaded);
                }

                return lastAvailable;
            }
        }

        public bool ExecuteLiCommand(string arguments)
        {
            try
            {
                executeCommandSubscriber.InvokeAction(arguments);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.PluginLog.Warning($"Lifestream IPC call failed: {ex.Message}");
                return false;
            }
        }
    }
}
