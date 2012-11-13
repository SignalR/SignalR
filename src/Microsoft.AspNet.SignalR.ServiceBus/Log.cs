// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Diagnostics;

    static class Log
    {
        public static void MessageDispatcherDequeueException(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessageDispather failed to dequeue. {0}", exception);
        }

        public static void MessageDispatcherErrorInCallback(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessageDispathcer failed to call the user callback. Exception = {0}", exception);
        }

        public static void MessagePumpReceiveException(string id, Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessagePump({0} failed to recieve messages. Exception = {1}", id, exception);
        }

        public static void MessagePumpUnexpectedException(string id, Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessagePump({0}) encountered an unexpected exception. Exception = {1}", id, exception);
        }

        public static void MessageDispatcherUnexpectedException(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessageDispatcher encountered an unexpected exception. Exception = {0}", exception);
        }

        public static void MessagePumpBackoff(TimeSpan amount, Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessagePump is sleeping due to an exception. Amount = {0}, Exception = {1}", amount, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static void QueueMessageBusInitializationFailure(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "QueueMessageBuffer failed to initialize. Exception = {0}", exception);
        }

        public static void TopicMessagePumpInitializationFailed(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "TopicMessageBuffer failed to initialize. Exception = {0}", exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static void QueueMessageBusSendFailure(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "QueueMessageBuffer failed to send. Exception = {0}", exception);
        }

        public static void TopicMessageBusSendFailure(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "TopicMessageBuffer failed to send. Exception = {0}", exception);
        }

        public static void MessagePumpDeserializationException(Exception exception)
        {
            TraceWriteLine(TraceEventType.Warning, "MessagePump failed to deserialize messages. Exception = {0}", exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])", Justification = "We're not localizing trace messages.")]
        static void TraceWriteLine(TraceEventType level, string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args), level.ToStringFast());
        }
    }
}
