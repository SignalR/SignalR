// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Settings for the SQL Server scale-out message bus implementation.
    /// </summary>
    public class SqlScaleOutConfiguration : ScaleOutConfiguration
    {
        public SqlScaleOutConfiguration()
        {
            TableCount = 1;
        }

        /// <summary>
        /// The number of tables to store messages in. Using more tables reduces lock contention and can increase throughput.
        /// Defaults to 1.
        /// </summary>
        public uint TableCount { get; set; }
    }
}
