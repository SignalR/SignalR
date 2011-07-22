using System;
using System.Web;

namespace SignalR.SignalBuses {
    public class SignalReceiverHandler : IHttpHandler {
        public static string HandlerName { get; set; }

        internal static class QueryStringKeys {
            internal static readonly string LoopbackTest = "loopbackTest";
            internal static readonly string EventKey = "eventKey";
        }

        internal static class ResponseValues {
            internal static readonly string Self = "self";
            internal static readonly string Ack = "ack";
        }

        static SignalReceiverHandler() {
            HandlerName = "SignalReceiver.axd";
        }

        public bool IsReusable {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return false; }
        }

        public void ProcessRequest(HttpContext context) {
            var signalBus = Signaler.Instance.SignalBus as PeerToPeerHttpSignalBus;
            if (signalBus == null) {
                return;
            }

            var eventKey = context.Request.QueryString[SignalReceiverHandler.QueryStringKeys.EventKey];
            if (!String.IsNullOrEmpty(eventKey)) {
                signalBus.SignalReceived(eventKey);
                return;
            }

            var loopbackTest = context.Request.QueryString[SignalReceiverHandler.QueryStringKeys.LoopbackTest];
            if (!String.IsNullOrEmpty(loopbackTest)) {
                // Loopback test
                Guid id;
                Guid.TryParse(loopbackTest, out id);
                context.Response.Write(id == signalBus.Id ? SignalReceiverHandler.ResponseValues.Self : SignalReceiverHandler.ResponseValues.Ack);
            }
        }
    }
}