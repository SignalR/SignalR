using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisScaleoutConfiguration : ScaleoutConfiguration
    {
        public Func<RedisConnection> ConnectionFactory { get; set; }

        public int Database { get; set; }

        public string EventKey { get; set; }

        public static RedisScaleoutConfiguration Create(string server, int port, string password)
        {
            return new RedisScaleoutConfiguration
            {
                ConnectionFactory = MakeConnectionFactory(server, port, password)
            };
        }

        private static Func<RedisConnection> MakeConnectionFactory(string server, int port, string password)
        {
            return () => new RedisConnection(server, port: port, password: password);
        }
    }
}
