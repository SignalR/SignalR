using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Client
{
    public interface IConnection
    {
        bool IsActive { get; }
        long? MessageId { get; set; }
        Func<string> Sending { get; set; }
        IEnumerable<string> Groups { get; }
        string ClientId { get; }
        string Url { get; }

        event Action Closed;
        event Action<Exception> Error;
        event Action<string> Received;

        void Stop();
        Task Send(string data);
        Task<T> Send<T>(string data);
    }
}
