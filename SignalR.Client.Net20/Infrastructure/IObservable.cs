using System;
using System.IO;

namespace SignalR.Client.Net20.Infrastructure
{
    public interface IObserver<T>
    {
        void OnNext(T value);
        void OnCompleted();
        void OnError(Exception exception);
    }

    internal class ReadStreamState
    {
        public Stream Stream { get; set; }
        public byte[] Buffer { get; set; }
        public Task<int> Response { get; set; }
    }

    internal class WriteStreamState
    {
        public Stream Stream { get; set; }
        public byte[] Buffer { get; set; }
        public Task Response { get; set; }
    }
}
