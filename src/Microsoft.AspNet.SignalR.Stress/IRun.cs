using System;

namespace Microsoft.AspNet.SignalR.Stress
{
    public interface IRun : IDisposable
    {
        int Warmup { get; }

        int Duration { get; }

        void Run();

        void Sample();

        void Record();
    }
}
