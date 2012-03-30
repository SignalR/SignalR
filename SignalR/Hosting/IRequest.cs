using System;
using System.Collections.Specialized;

namespace SignalR.Hosting
{
    public interface IRequest
    {
        Uri Url { get; }
        NameValueCollection QueryString { get; }
        NameValueCollection Headers { get; }
        NameValueCollection Form { get; }
        IRequestCookieCollection Cookies { get; }
    }
}
