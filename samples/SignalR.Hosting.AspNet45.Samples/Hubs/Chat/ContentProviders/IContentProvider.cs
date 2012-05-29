using System.Net;

namespace SignalR.Samples.Hubs.Chat
{
    public interface IContentProvider
    {
        string GetContent(HttpWebResponse response);
    }
}