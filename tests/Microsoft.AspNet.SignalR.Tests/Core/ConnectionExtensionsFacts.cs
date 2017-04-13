// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class ConnectionExtensionsFacts
    {
        [Fact]
        public async Task SendThrowsNullExceptionWhenConnectionIdIsNull()
        {
            var serializer = JsonUtility.CreateDefaultSerializer();
            var counters = new PerformanceCounterManager();

            var connection = new Connection(new Mock<IMessageBus>().Object,
                                serializer,
                                "signal",
                                "connectonid",
                                new[] { "test" },
                                new string[] { },
                                new Mock<ITraceManager>().Object,
                                new AckHandler(completeAcksOnTimeout: false,
                                               ackThreshold: TimeSpan.Zero,
                                               ackInterval: TimeSpan.Zero),
                                counters,
                                new Mock<IProtectedData>().Object,
                                new MemoryPool());

            await Assert.ThrowsAsync<ArgumentException>(() => connection.Send((string)null, new object()));
        }

        [Fact]
        public async Task SendThrowsNullExceptionWhenConnectionIdsAreNull()
        {
            var serializer = JsonUtility.CreateDefaultSerializer();
            var counters = new PerformanceCounterManager();

            var connection = new Connection(new Mock<IMessageBus>().Object,
                                serializer,
                                "signal",
                                "connectonid",
                                new[] { "test" },
                                new string[] { },
                                new Mock<ITraceManager>().Object,
                                new AckHandler(completeAcksOnTimeout: false,
                                               ackThreshold: TimeSpan.Zero,
                                               ackInterval: TimeSpan.Zero),
                                counters,
                                new Mock<IProtectedData>().Object,
                                new MemoryPool());

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => connection.Send((IList<string>)null, new object()));
        }
    }
}
