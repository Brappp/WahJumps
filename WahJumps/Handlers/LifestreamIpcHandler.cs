using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace WahJumps.Handlers
{
    public class LifestreamIpcHandler
    {
        private readonly ICallGateSubscriber<string, object> executeCommandSubscriber;

        public LifestreamIpcHandler(IDalamudPluginInterface pluginInterface)
        {
            executeCommandSubscriber = pluginInterface.GetIpcSubscriber<string, object>("Lifestream.ExecuteCommand");
        }

        // False if Lifestream isn't available, so callers can react instead of throwing.
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
