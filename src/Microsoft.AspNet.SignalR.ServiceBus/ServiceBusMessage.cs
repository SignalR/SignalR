// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public static class ServiceBusMessage
    {
        private static readonly JsonSerializer _serializer = GetSerializer();

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The stream is returned to the caller of ths method")]
        public static Stream ToStream(IList<Message> messages)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            _serializer.Serialize(writer, messages);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static IList<Message> FromStream(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return _serializer.Deserialize<Message[]>(jsonReader);
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
