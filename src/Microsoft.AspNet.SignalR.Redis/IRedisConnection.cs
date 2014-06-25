using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Redis
{
    public interface IRedisConnection
    {
        Task ConnectAsync(string connectionString);

        void Close(bool allowCommandsToComplete = true);

        Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage);

        Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments);

        void Dispose();

        event Action<Exception> ConnectionFailed;
        event Action<Exception> ConnectionRestored;
        event Action<Exception> ErrorMessage;
    }
}
