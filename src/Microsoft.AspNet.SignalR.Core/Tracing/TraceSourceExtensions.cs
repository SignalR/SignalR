// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics;

namespace System.Diagnostics
{
    public static class TraceSourceExtensions
    {
        public static void TraceVerbose(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Verbose, message);
        }

        public static void TraceVerbose(this TraceSource traceSource, string message, params object[] args)
        {
            Trace(traceSource, TraceEventType.Verbose, message, args);
        }

        public static void TraceError(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Error, message);
        }

        public static void TraceError(this TraceSource traceSource, string message, params object[] args)
        {
            Trace(traceSource, TraceEventType.Error, message, args);
        }

        private static void Trace(TraceSource traceSource, TraceEventType eventType, string message)
        {
            traceSource.TraceEvent(eventType, 0, message);
        }

        private static void Trace(TraceSource traceSource, TraceEventType eventType, string message, params object[] args)
        {
            traceSource.TraceEvent(eventType, 0, message, args);
        }
    }
}
