using System;

namespace Microsoft.AspNet.SignalR.Stress
{
    public interface IRun : IDisposable
    {
        void Run();
    }
}
