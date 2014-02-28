using System.Net;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Chat
{
    public interface IContentProvider
    {
        string GetContent(HttpWebResponse response);
    }
}