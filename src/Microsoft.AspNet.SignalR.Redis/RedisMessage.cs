// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Redis
{
    [Serializable]
    public class RedisMessage
    {
        public RedisMessage(long id, Message[] message)
        {
            Id = id;
            Messages = message;
        }

        public long Id { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This type is used for seriaization")]
        public Message[] Messages { get; set; }

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
