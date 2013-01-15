// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

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
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Notify_InstallPerformanceCounters); }
        }

        public override string Help
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Notify_InstallSignalRPerformanceCounters); }
        }

        public override string[] Names
        {
            get { return new[] { "ipc" }; }
        }

        public override void Execute(string[] args)
        {
            Info(String.Format(CultureInfo.CurrentCulture, Resources.Notify_InstallingPerformanceCounters));

            var installer = new PerformanceCounterInstaller();
            IList<string> counters;

            try
            {
                counters = installer.InstallCounters();
            }
            catch (UnauthorizedAccessException ex)
            {
                // Probably due to not running as admin, let's just stop here
                Warning(String.Format(CultureInfo.CurrentCulture, ex.Message + Resources.Notify_TryRunningAsAdmin));
                return;
            }

            foreach (var counter in counters)
            {
                Info("  " + counter);
            }

            Success(String.Format(CultureInfo.CurrentCulture, Resources.Notify_PerformanceCountersInstalled));
        }
    }
}
