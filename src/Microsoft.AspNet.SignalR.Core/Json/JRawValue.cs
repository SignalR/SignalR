﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        public JRawValue(JRaw value)
        {
            _value = value.ToString();
        }

        public object ConvertTo(Type type)
        {
            // A non generic implementation of ToObject<T> on JToken
            using (var jsonReader = new StringReader(_value))
            {
                var serializer = JsonUtility.CreateDefaultSerializer();
                return serializer.Deserialize(jsonReader, type);
            }
        }

        public bool CanConvertTo(Type type)
        {
            // TODO: Implement when we implement better method overload resolution
            return true;
        }
    }
}
