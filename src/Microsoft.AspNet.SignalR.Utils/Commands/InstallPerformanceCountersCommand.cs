using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Utils
{
    internal class InstallPerformanceCountersCommand : Command
    {
        public InstallPerformanceCountersCommand(Action<string> info, Action<string> success, Action<string> warning, Action<string> error)
            : base(info, success, warning, error)
        {

        }

        public override string DisplayName
        {
            get { return "Install Performance Counters"; }
        }

        public override string Help
        {
            get { return "Installs SignalR performance counters."; }
        }

        public override string[] Names
        {
            get { return new [] { "ipc" }; }
        }

        public override void Execute(string[] args)
        {
            Info("Installing performance counters...");

            var installer = new PerformanceCounterInstaller();
            IList<string> counters;

            try
            {
                counters = installer.InstallCounters();
            }
            catch (UnauthorizedAccessException ex)
            {
                // Probably due to not running as admin, let's just stop here
                Warning(ex.Message + " Try running as admin.");
                return;
            }

            foreach (var counter in counters)
            {
                Info("  " + counter);
            }

            Success("Performance counters installed!");
        }
    }
}
