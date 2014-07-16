using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Redis
{
    public interface IRedisConnection
    {
        Task ConnectAsync(string connectionString, TraceSource trace);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void Close(string key, bool allowCommandsToComplete = true);

        Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage);

        Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments);

        Task RestoreLatestValueForKey(int database, string key);

        void Dispose();

        event Action<Exception> ConnectionFailed;
        event Action<Exception> ConnectionRestored;
        event Action<Exception> ErrorMessage;
    }
}
