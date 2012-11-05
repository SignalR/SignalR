// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR
{
    public class Command
    {
        public Command()
        {
            Id = Guid.NewGuid().ToString();
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ack", Justification = "ACK is a well known networking term.")]
        public bool WaitForAck { get; set; }
        public string Id { get; private set; }
        public CommandType CommandType { get; set; }
        public string Value { get; set; }
    }
}
