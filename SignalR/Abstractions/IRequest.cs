using System.Collections.Specialized;
using System;

namespace SignalR.Abstractions
{
    public interface IRequest
    {
        Uri Url { get; }
        NameValueCollection QueryString { get; }
        NameValueCollection Headers { get; }
        NameValueCollection Form { get; }
        NameValueCollection Cookies { get; }
    }
}
