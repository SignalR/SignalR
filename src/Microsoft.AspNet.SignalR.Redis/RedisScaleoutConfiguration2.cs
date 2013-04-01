using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookSleeve;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisScaleoutConfiguration2 : ScaleoutConfiguration
    {
        public RedisScaleoutConfiguration2(string server, int port, string password, string eventKey)
            : this(MakeConnectionFactory(server, port, password), eventKey)
        {

        }

        public RedisScaleoutConfiguration2(Func<RedisConnection> connectionFactory, string eventKey)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            if (eventKey == null)
            {
                throw new ArgumentNullException("eventKey");
            }

            ConnectionFactory = connectionFactory;
            EventKey = eventKey;
        }

        public Func<RedisConnection> ConnectionFactory { get; set; }

        public int Database { get; set; }

        public string EventKey { get; set; }

        private static Func<RedisConnection> MakeConnectionFactory(string server, int port, string password)
        {
            return () => new RedisConnection(server, port: port, password: password);
        }
    }
}
