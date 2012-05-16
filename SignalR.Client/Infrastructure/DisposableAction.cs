using System;
#if NET20
using Newtonsoft.Json.Serialization;
#endif

namespace SignalR.Client.Infrastructure
{
    internal class DisposableAction : IDisposable
    {
        private readonly Action _action;
        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
