// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessage
    {
        private static readonly JsonSerializer _serializer = GetSerializer();

        public RedisMessage(long id, IList<Message> messages)
        {
            Id = id;
            Messages = messages;
        }

        [JsonProperty("I")]
        public long Id { get; set; }

        [JsonProperty("M")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Type is used for serialization")]
        public IList<Message> Messages { get; set; }

        public static byte[] ToBytes(long id, IList<Message> messages)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                _serializer.Serialize(writer, new RedisMessage(id, messages));
                return Encoding.UTF8.GetBytes(writer.ToString());
            }
        }

        public static RedisMessage FromBytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var streamReader = new StreamReader(stream);
                var jsonReader = new JsonTextReader(streamReader);
                return _serializer.Deserialize<RedisMessage>(jsonReader);
            }
        }

        private static JsonSerializer GetSerializer()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MaxDepth = 20
            };

            return JsonSerializer.Create(settings);
        }
    }
}
