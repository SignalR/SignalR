using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Redis
{
    public interface IRedisConnection
    {
        Task ConnectAsync(string connectionString);

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParameter should not used", Justification = "This is to match external API")]
        void Close(bool allowCommandsToComplete = true);

        Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage);

        Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments);
        Task RestoreLatestValueForKey(TraceSource trace);
        void Dispose();

        event Action<Exception> ConnectionFailed;
        event Action<Exception> ConnectionRestored;
        event Action<Exception> ErrorMessage;
    }
}
