﻿using System.Threading.Tasks;

namespace SignalR.Client.Transports
{
    public interface IClientTransport
    {
        Task<NegotiationResponse> Negotiate(IConnection connection);
        Task Start(IConnection connection, string data);
        Task<T> Send<T>(IConnection connection, string data);
        void Stop(IConnection connection);
    }
}
