// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace System.Diagnostics
{
    public static class TraceSourceExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "msg")]
        public static TraceSource TraceVerbose(this TraceSource traceSource, string msg)
        {
            Trace(traceSource, TraceEventType.Verbose, msg);
            return traceSource;
        }

        public static TraceSource TraceVerbose(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Verbose, format, args);
            return traceSource;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "msg")]
        public static TraceSource TraceWarning(this TraceSource traceSource, string msg)
        {
            Trace(traceSource, TraceEventType.Warning, msg);
            return traceSource;
        }

        public static TraceSource TraceWarning(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Warning, format, args);
            return traceSource;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "msg")]
        public static TraceSource TraceError(this TraceSource traceSource, string msg)
        {
            Trace(traceSource, TraceEventType.Error, msg);
            return traceSource;
        }

        public static TraceSource TraceError(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Error, format, args);
            return traceSource;
        }

        private static TraceSource Trace(TraceSource traceSource, TraceEventType eventType, string msg)
        {
            traceSource.TraceEvent(eventType, 0, msg);
            return traceSource;
        }

        private static TraceSource Trace(TraceSource traceSource, TraceEventType eventType, string format, params object[] args)
        {
            traceSource.TraceEvent(eventType, 0, format, args);
            return traceSource;
        }
    }
}
