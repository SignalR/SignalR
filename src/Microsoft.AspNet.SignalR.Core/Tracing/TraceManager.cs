// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Tracing
{
    public class TraceManager : ITraceManager
    {
        private readonly ConcurrentDictionary<string, TraceSource> _sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);
        private readonly TextWriterTraceListener _hostTraceListener;

        private static readonly string SwitchName = "SignalRSwitch";

        /// <summary>
        /// Gets a default trace manager that will only trace to the globally-registered listeners.
        /// </summary>
        public static readonly ITraceManager Default = new TraceManager();

        public TraceManager()
            : this(hostTraceListener: null)
        {
        }

        public TraceManager(TextWriterTraceListener hostTraceListener)
        {
            Switch = new SourceSwitch(SwitchName);
            _hostTraceListener = hostTraceListener;
        }

        public SourceSwitch Switch { get; private set; }

        public TraceSource this[string name]
        {
            get
            {
                return _sources.GetOrAdd(name, key => CreateTraceSource(key));
            }
        }

        private TraceSource CreateTraceSource(string name)
        {
            var traceSource = new TraceSource(name, SourceLevels.Off)
            {
                Switch = Switch
            };

            if (_hostTraceListener != null)
            {
                if (traceSource.Listeners.Count > 0 &&
                    traceSource.Listeners[0] is DefaultTraceListener)
                {
                    traceSource.Listeners.RemoveAt(0);
                }

                traceSource.Listeners.Add(_hostTraceListener);
            }

            return traceSource;
        }
    }
}
