using System;
using System.IO;
using System.Web;

namespace SignalR {
    public class MessageReceiverHandler : IHttpHandler {
        public static string HandlerName { get; set; }

        internal static class Keys {
            internal static readonly string LoopbackTest = "loopbackTest";
            internal static readonly string EventKey = "eventKey";
            internal static readonly string Message = "message";
        }

        internal static class ResponseValues {
            internal static readonly string Self = "self";
            internal static readonly string Ack = "ack";
        }

        static MessageReceiverHandler() {
            HandlerName = "MessageReceiver.axd";
        }

        public bool IsReusable {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return false; }
        }

        public void ProcessRequest(HttpContext context) {
            var signalBus = Signaler.Instance.SignalBus as PeerToPeerSQLSignalBusMessageStore;
            if (signalBus == null) {
                return;
            }

            var eventKey = context.Request.QueryString[MessageReceiverHandler.Keys.EventKey];
            if (!String.IsNullOrEmpty(eventKey)) {
                var payload = context.Request.Form[MessageReceiverHandler.Keys.Message];
                if (!String.IsNullOrEmpty(payload)) {
                    signalBus.MessageReceived(payload);
                    return;
                }
            }

            var loopbackTest = context.Request.QueryString[MessageReceiverHandler.Keys.LoopbackTest];
            if (!String.IsNullOrEmpty(loopbackTest)) {
                // Loopback test
                Guid id;
                Guid.TryParse(loopbackTest, out id);
                context.Response.Write(id == signalBus.Id ? MessageReceiverHandler.ResponseValues.Self : MessageReceiverHandler.ResponseValues.Ack);
            }
        }
    }
}