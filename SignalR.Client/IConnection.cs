using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SignalR.Client
{
    public interface IConnection
    {
        bool IsActive { get; }
        long? MessageId { get; set; }
        Func<string> Sending { get; set; }
        IEnumerable<string> Groups { get; }
        IDictionary<string, object> Items { get; }
        string ConnectionId { get; }
        string Url { get; }

        ICredentials Credentials { get; set; }
        CookieContainer CookieContainer { get; set; }

        event Action Closed;
        event Action<Exception> Error;
        event Action<string> Received;

        void Stop();
        Task Send(string data);
        Task<T> Send<T>(string data);
    }
}
