using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Redis;

namespace Microsoft.AspNet.SignalR.Tests.Scaleout
{
    public class FakeRedisConnection : IRedisConnection
    {
        public virtual Task ConnectAsync(string connectionString)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual void Close(bool allowCommandsToComplete = true)
        {
        }

        public virtual Task SubscribeAsync(string key, Action<int, RedisMessage> onMessage)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual Task ScriptEvaluateAsync(int database, string script, string key, byte[] messageArguments)
        {
            return TaskAsyncHelper.Empty;
        }

        public virtual void Dispose()
        {
            // ConnectionRestored += (ex) => { };
            throw new NotImplementedException();
        }

        public virtual event Action<Exception> ConnectionFailed = ex =>
        {
            Console.ReadLine();
        };

        public virtual event Action<Exception> ConnectionRestored;

        public virtual event Action<Exception> ErrorMessage = ex => { };
    }
}
