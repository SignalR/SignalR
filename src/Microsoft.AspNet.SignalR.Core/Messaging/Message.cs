// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    [Serializable]
    public class Message
    {
        // Core properties
        public string Source { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public string CommandId { get; set; }
        public bool WaitForAck { get; set; }
        public bool IsAck { get; set; }
        public string Filter { get; set; }

        public bool IsCommand
        {
            get
            {
                return CommandId != null;
            }
        }

        public Message()
        {
        }

        public Message(string source, string key, string value)
        {
            Source = source;
            Key = key;
            Value = value;
        }
    }
}
