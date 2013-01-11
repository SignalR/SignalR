using System;
using System.Collections.Specialized;
using Microsoft.AspNet.SignalR.Hosting;
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
            public void NonGuidConnectionIdThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                var qs = new NameValueCollection();
                qs["transport"] = "serverSentEvents";
                qs["connectionId"] = "1";
                req.Setup(m => m.QueryString).Returns(qs);

                var dr = new DefaultDependencyResolver();
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr, context);

                Assert.Throws<InvalidOperationException>(() => connection.Object.ProcessRequest(context));
            }
        }
    }
}
