using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace SignalR.ScaleOut
{
    internal static class PeerToPeerHelper
    {
        public static class RequestKeys
        {
            internal static readonly string LoopbackTest = "loopbackTest";
            internal static readonly string EventKey = "eventKey";
            internal static readonly string Message = "message";
        }

        public static class ResponseValues
        {
            internal static readonly string Self = "self";
            internal static readonly string Ack = "ack";
        }

        public static void EnsurePeersDiscovered(ref bool peersDiscovered, IPeerUrlSource urls, ICollection<string> peers, string handlerName, Guid selfId, object locker, Action<HttpWebRequest> requestPreparer)
        {
            if (peersDiscovered)
            {
                return;
            }

            lock (locker)
            {
                if (peersDiscovered)
                {
                    return;
                }
                foreach (var url in urls.GetPeerUrls())
                {
                    peers.Add(url);
                }
                // Loop through peers and send the loopbackTest
                var queryString = "?" + RequestKeys.LoopbackTest + "=" + HttpUtility.UrlEncode(selfId.ToString());
                var peersToRemove = new List<string>();
                Parallel.ForEach(peers, peer =>
                {
                    try
                    {
                        CreatePrepareAndSendRequestAsync(peer + handlerName + queryString, requestPreparer)
                            .ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    peersToRemove.Add(peer);
                                }
                                else
                                {
                                    using (var sr = new StreamReader(t.Result.GetResponseStream()))
                                    {
                                        var result = sr.ReadToEnd();
                                        if (result != ResponseValues.Ack)
                                        {
                                            // Something wrong with peer, or is ourself, so remove from list
                                            peersToRemove.Add(peer);
                                        }
                                    }
                                }
                            })
                            .Wait();
                    }
                    catch (Exception)
                    {
                        // Problem contacting peer, remove it from the list
                        peersToRemove.Add(peer);
                    }
                });
                if (peersToRemove.Count > 0)
                {
                    peersToRemove.ForEach(p => peers.Remove(p));
                }
                peersDiscovered = true;
            }
        }

        public static Task<HttpWebResponse> CreatePrepareAndSendRequestAsync(string url, Action<HttpWebRequest> requestPreparer)
        {
            return HttpHelper.GetAsync(url, requestPreparer);
        }
    }
}