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

        public void ExecuteLiCommand(string arguments)
        {
            executeCommandSubscriber.InvokeAction(arguments);
        }
    }
}
