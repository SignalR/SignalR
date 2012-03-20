using System.Net;

namespace SignalR.Client.Infrastructure
{
    public interface IHttpRequest
    {
        string UserAgent { get; set; }
        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }
        string Accept { get; set; }

        void Abort();
    }
}
