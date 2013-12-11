using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public static class SystemNetLogging
    {
        private static readonly Lazy<Logging> _logging = new Lazy<Logging>(() => new Logging());

        public static IDisposable Enable(string path)
        {
            var listener = new TextWriterTraceListener(path);
            _logging.Value.Sources.ForEach(s => s.Listeners.Add(listener));

            return new DisposableAction(() =>
            {
                listener.Flush();
                _logging.Value.Sources.ForEach(s => s.Listeners.Remove(listener));
                listener.Close();
            });
        }

        private class Logging
        {
            private readonly Type _loggingType;

            public List<TraceSource> Sources = new List<TraceSource>();

            public Logging()
            {
                _loggingType = Type.GetType("System.Net.Logging, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

                if (_loggingType == null)
                {
                    return;
                }

                var webProperty = _loggingType.GetProperty("Web", BindingFlags.NonPublic | BindingFlags.Static);
                if (webProperty != null)
                {
                    Sources.Add((TraceSource)webProperty.GetValue(null));
                }

                var socketsProperty = _loggingType.GetProperty("Sockets", BindingFlags.NonPublic | BindingFlags.Static);
                if (socketsProperty != null)
                {
                    Sources.Add((TraceSource)socketsProperty.GetValue(null));
                }

                var webSocketsProperty = _loggingType.GetProperty("WebSockets", BindingFlags.NonPublic | BindingFlags.Static);
                if (webSocketsProperty != null)
                {
                    Sources.Add((TraceSource)webSocketsProperty.GetValue(null));
                }
            }
        }
    }
}
