// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR
{
    [Serializable]
    public class Message
    {
        // Core properties
        public string Source { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Id { get; set; }

        // Replies
        public string AckId { get; set; }
        public bool WaitForAck { get; set; }

        public bool IsAck
        {
            get
            {
                return AckId != null;
            }
        }

        // Filtering
        public string Filter { get; set; }
        public bool IsCommand { get; set; }


        public Message()
        {
        }

        public Message(string source, string key, string value)
        {
            Id = Guid.NewGuid().ToString();
            Source = source;
            Key = key;
            Value = value;
        }
    }
}
