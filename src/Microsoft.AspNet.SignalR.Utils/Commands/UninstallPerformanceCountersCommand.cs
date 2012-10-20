using System;

namespace Microsoft.AspNet.SignalR.Utils
{
    internal class UninstallPerformanceCountersCommand : Command
    {
        public UninstallPerformanceCountersCommand(Action<string> info, Action<string> success, Action<string> warning, Action<string> error)
            : base(info, success, warning, error)
        {

        }

        public override string DisplayName
        {
            get { return "Uninstall Performance Counters"; }
        }

        public override string Help
        {
            get { return "Uninstalls SignalR performance counters."; }
        }

        public override string[] Names
        {
            get { return new[] { "upc" }; }
        }

        public override void Execute(string[] args)
        {
            var installer = new PerformanceCounterInstaller();
            installer.UninstallCounters();

            Success("Performance counters uninstalled!");
        }
    }
}
