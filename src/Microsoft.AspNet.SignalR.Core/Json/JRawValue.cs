// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// An implementation of IJsonValue over JSON.NET
    /// </summary>
    internal class JRawValue : IJsonValue
    {
        private readonly string _value;
        private readonly JsonSerializer _serializer;

        public JRawValue(JRaw value, JsonSerializer serializer)
        {
            _value = value.ToString();
            _serializer = serializer;
        }

        public object ConvertTo(Type type)
        {
            // A non generic implementation of ToObject<T> on JToken
            using (var jsonReader = new StringReader(_value))
            {
                return _serializer.Deserialize(jsonReader, type);
            }
        }

        public bool CanConvertTo(Type type)
        {
            // TODO: Implement when we implement better method overload resolution
            return true;
        }
    }
}
