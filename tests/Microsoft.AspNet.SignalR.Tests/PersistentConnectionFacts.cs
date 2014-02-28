using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
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
                Assert.Throws<ArgumentNullException>(() => connection.Object.ProcessRequest((HostContext)null));
            }

            [Fact]
            public void UninitializedThrows()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                Assert.Throws<InvalidOperationException>(() => connection.Object.ProcessRequest(new HostContext(null, null)));
            }

            [Fact]
            public void UnknownTransportFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                req.Setup(m => m.LocalPath).Returns("");
                var qs = new NameValueCollection();
                req.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));

                var res = new Mock<IResponse>();
                res.SetupProperty(m => m.StatusCode);

                var dr = new DefaultDependencyResolver();
                var context = new HostContext(req.Object, res.Object);
                connection.Object.Initialize(dr);

                var task = connection.Object.ProcessRequest(context);

                Assert.True(task.IsCompleted);
                Assert.Equal(400, context.Response.StatusCode);
            }

            [Fact]
            public void MissingConnectionTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                req.Setup(m => m.LocalPath).Returns("");
                var qs = new NameValueCollection();
                qs["transport"] = "serverSentEvents";
                req.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));

                var res = new Mock<IResponse>();
                res.SetupProperty(m => m.StatusCode);

                var dr = new DefaultDependencyResolver();
                var context = new HostContext(req.Object, res.Object);
                connection.Object.Initialize(dr);

                var task = connection.Object.ProcessRequest(context);

                Assert.True(task.IsCompleted);
                Assert.Equal(400, context.Response.StatusCode);
            }
        }

        public class VerifyGroups
        {
            [Fact]
            public void MissingGroupTokenReturnsEmptyList()
            {
                var groups = DoVerifyGroups(groupsToken: null, connectionId: null);

                Assert.Equal(0, groups.Count);
            }

            [Fact]
            public void NullProtectedDataTokenReturnsEmptyList()
            {
                var groups = DoVerifyGroups(groupsToken: "groups", connectionId: null, hasProtectedData: false);

                Assert.Equal(0, groups.Count);
            }

            [Fact]
            public void GroupsTokenWithInvalidConnectionIdReturnsEmptyList()
            {
                var groups = DoVerifyGroups(groupsToken: @"wrong:[""g1"",""g2""]", connectionId: "id");

                Assert.Equal(0, groups.Count);
            }

            [Fact]
            public void GroupsAreParsedFromToken()
            {
                var groups = DoVerifyGroups(groupsToken: @"id:[""g1"",""g2""]", connectionId: "id");

                Assert.Equal(2, groups.Count);
                Assert.Equal("g1", groups[0]);
                Assert.Equal("g2", groups[1]);
            }

            private static IList<string> DoVerifyGroups(string groupsToken, string connectionId, bool hasProtectedData = true)
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.Url).Returns(new Uri("http://foo"));
                req.Setup(m => m.LocalPath).Returns("");
                var qs = new NameValueCollection();
                qs["transport"] = "serverSentEvents";
                qs["connectionToken"] = "1";
                qs["groupsToken"] = groupsToken;

                req.Setup(m => m.QueryString).Returns(new NameValueCollectionWrapper(qs));

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => hasProtectedData ? value : null);

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr);

                return connection.Object.VerifyGroups(context, connectionId);
            }
        }

        public class GetConnectionId
        {
            [Fact]
            public void UnprotectedConnectionTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Throws<InvalidOperationException>();

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(false, connection.Object.TryGetConnectionId(context, "1", out connectionId, out message, out statusCode));
                Assert.Equal(null, connectionId);
                Assert.Equal(400, statusCode);
            }

            [Fact]
            public void NullUnprotectedConnectionTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns((string)null);

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(false, connection.Object.TryGetConnectionId(context, "1", out connectionId, out message, out statusCode));
                Assert.Equal(null, connectionId);
                Assert.Equal(400, statusCode);
            }

            [Fact]
            public void UnauthenticatedUserWithAuthenticatedTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, purpose) => value);

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(false, connection.Object.TryGetConnectionId(context, "1:::11:::::::1:1", out connectionId, out message, out statusCode));
                Assert.Equal(403, statusCode);
            }

            [Fact]
            public void AuthenticatedUserNameMatches()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.User).Returns(new GenericPrincipal(new GenericIdentity("Name"), new string[] { }));

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, purpose) => value);

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(true, connection.Object.TryGetConnectionId(context, "1:Name", out connectionId, out message, out statusCode));
                Assert.Equal("1", connectionId);
            }

            [Fact]
            public void AuthenticatedUserWithColonsInUserName()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var req = new Mock<IRequest>();
                req.Setup(m => m.User).Returns(new GenericPrincipal(new GenericIdentity("::11:::::::1:1"), new string[] { }));

                string connectionId = Guid.NewGuid().ToString("d");

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, purpose) => value);

                var dr = new DefaultDependencyResolver();
                dr.Register(typeof(IProtectedData), () => protectedData.Object);
                var context = new HostContext(req.Object, null);
                connection.Object.Initialize(dr);

                string cid;
                string message;
                int statusCode;

                Assert.Equal(true, connection.Object.TryGetConnectionId(context, connectionId + ":::11:::::::1:1", out cid, out message, out statusCode));
                Assert.Equal(connectionId, cid);
            }
        }
    }
}
