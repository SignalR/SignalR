using System;
using System.Web;
using SignalR.ScaleOut;

namespace SignalR.Web
{
    public class MessageReceiverHandler : IHttpHandler
    {
        public static string HandlerName { get; set; }

        static MessageReceiverHandler()
        {
            HandlerName = "MessageReceiver.axd";
        }

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var signalBus = Signaler.Instance.SignalBus as PeerToPeerSQLSignalBusMessageStore;
            if (signalBus == null)
            {
                return;
            }

            var eventKey = context.Request.QueryString[PeerToPeerHelper.RequestKeys.EventKey];
            if (!String.IsNullOrEmpty(eventKey))
            {
                var payload = context.Request.Form[PeerToPeerHelper.RequestKeys.Message];
                if (!String.IsNullOrEmpty(payload))
                {
                    // REVIEW: Should we return the Task here i.e. use HttpTaskAsyncHandler instead?
                    signalBus.MessageReceived(payload);
                    return;
                }
            }

            var loopbackTest = context.Request.QueryString[PeerToPeerHelper.RequestKeys.LoopbackTest];
            if (!String.IsNullOrEmpty(loopbackTest))
            {
                // Loopback test
                Guid id;
                Guid.TryParse(loopbackTest, out id);
                context.Response.Write(id == signalBus.Id ? PeerToPeerHelper.ResponseValues.Self : PeerToPeerHelper.ResponseValues.Ack);
            }
        }
    }
}