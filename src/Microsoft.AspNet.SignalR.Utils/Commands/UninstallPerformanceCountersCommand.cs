// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Globalization;

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
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Notify_UninstallPerformanceCounters); }
        }

        public override string Help
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Notify_UninstallSignalRPerformanceCounters); }
        }

        public override string[] Names
        {
            get { return new[] { "upc" }; }
        }

        public override void Execute(string[] args)
        {
            var installer = new PerformanceCounterInstaller();
            installer.UninstallCounters();

            Success(String.Format(CultureInfo.CurrentCulture, Resources.Notify_PerformanceCountersUninstalled));
        }
    }
}
