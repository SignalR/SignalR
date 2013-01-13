﻿using System;
using System.Collections.Specialized;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class PersistentConnectionFacts
    {
        public class ProcessRequest
        {
            [Fact]
            public void NullContextThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                Assert.Throws<ArgumentNullException>(() => connection.Object.ProcessRequest(null));
            }

            [Fact]
            public void UninitializedThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                Assert.Throws<InvalidOperationException>(() => connection.Object.ProcessRequest(new HostContext(null, null)));
            }

            [Fact]
            public void UnknownTransportThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                var qs = new NameValueCollection();
                req.Setup(m => m.QueryString).Returns(qs);

                var dr = new DefaultDependencyResolver();
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr, context);

                Assert.Throws<InvalidOperationException>(() => connection.Object.ProcessRequest(context));
            }

            [Fact]
            public void MissingConnectionIdThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                var qs = new NameValueCollection();
                qs["transport"] = "serverSentEvents";
                req.Setup(m => m.QueryString).Returns(qs);

                var dr = new DefaultDependencyResolver();
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr, context);

                Assert.Throws<InvalidOperationException>(() => connection.Object.ProcessRequest(context));
            }

            [Fact]
            public void UnprotectedConnectionIdThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                var qs = new NameValueCollection();
                qs["transport"] = "serverSentEvents";
                qs["connectionId"] = "1";
                req.Setup(m => m.QueryString).Returns(qs);

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Throws<InvalidOperationException>();

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr, context);

                Assert.Throws<InvalidOperationException>(() => connection.Object.ProcessRequest(context));
            }
        }
    }
}
