using System;
using System.Text;
using Newtonsoft.Json;

namespace SignalR.Redis
{
    [Serializable]
    public class RedisMessage
    {
        public RedisMessage(long id, Message[] message)
        {
            Id = id;
            Messages = message;
        }

        public long Id { get; private set; }
        public Message[] Messages { get; private set; }

        public byte[] GetBytes()
        {
            var s = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(s);
        }

        public static RedisMessage Deserialize(byte[] data)
        {
            var s = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<RedisMessage>(s);
        }
    }
}