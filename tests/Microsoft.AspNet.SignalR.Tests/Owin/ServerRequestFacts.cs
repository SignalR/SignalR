using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNet.SignalR.Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Owin
{
    public class ServerRequestFacts
    {
        [Fact]
        public void NoPortReturnsDefaultHttpPort()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "www.foo.com" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("www.foo.com", request.Url.Host);
            Assert.Equal(80, request.Url.Port);
        }

        [Fact]
        public void NoPortReturnsDefaultHttpsPort()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "https";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "www.foo.com" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("www.foo.com", request.Url.Host);
            Assert.Equal(443, request.Url.Port);
        }

        [Fact]
        public void UsesLocalPortIfHostHeaderMissing()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "https";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            env[OwinConstants.LocalPort] = "12345";
            env[OwinConstants.LocalIpAddress] = "192.168.1.1";
            var headers = new Dictionary<string, string[]>();
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("192.168.1.1", request.Url.Host);
            Assert.Equal(12345, request.Url.Port);
        }
        
        [Fact]
        public void NoHostHeaderUsesIPAddress()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            env[OwinConstants.RequestHeaders] = headers;
            env[OwinConstants.LocalIpAddress] = "someip";
            var request = new ServerRequest(env);

            Assert.Equal("someip", request.Url.Host);
            Assert.Equal(80, request.Url.Port);
        }

        [Fact]
        public void NoHostOrIpAddressUsesLocalhost()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "https";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("localhost", request.Url.Host);
            Assert.Equal(443, request.Url.Port);
        }

        [Fact]
        public void DomainForHostHeader()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "https";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "www.foo.com" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("www.foo.com", request.Url.Host);
            Assert.Equal(443, request.Url.Port);
        }

        [Fact]
        public void DomainForHostHeaderAndPort()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "https";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "www.foo.com:356" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("www.foo.com", request.Url.Host);
            Assert.Equal(356, request.Url.Port);
        }

        [Fact]
        public void IPv6AddressForHostHeader()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "[FEDC:BA98:7654:3210:FEDC:BA98:7654:3210]" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("[fedc:ba98:7654:3210:fedc:ba98:7654:3210]", request.Url.Host.ToLowerInvariant());
            Assert.Equal(80, request.Url.Port);
        }

        [Fact]
        public void IPv6AddressForHostHeaderAndPort()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "[FEDC:BA98:7654:3210:FEDC:BA98:7654:3210]:1234" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("[fedc:ba98:7654:3210:fedc:ba98:7654:3210]", request.Url.Host.ToLowerInvariant());
            Assert.Equal(1234, request.Url.Port);
        }

        [Fact]
        public void IPv4AddressForHostHeader()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "192.168.1.1" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("192.168.1.1", request.Url.Host);
            Assert.Equal(80, request.Url.Port);
        }

        [Fact]
        public void IPv4AddressAndPortForHostHeader()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "192.168.1.1:89" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("192.168.1.1", request.Url.Host);
            Assert.Equal(89, request.Url.Port);
        }

        [Fact]
        public void HostWithoutPortUsesDefaultHttpPort()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "http";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            env[OwinConstants.LocalPort] = "34";
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "www.foo.com" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("www.foo.com", request.Url.Host);
            Assert.Equal(80, request.Url.Port);
        }

        [Fact]
        public void HostWithoutPortUsesDefaultHttpsPort()
        {
            var env = new Dictionary<string, object>();
            env[OwinConstants.RequestScheme] = "https";
            env[OwinConstants.RequestPathBase] = String.Empty;
            env[OwinConstants.RequestPath] = String.Empty;
            env[OwinConstants.RequestQueryString] = String.Empty;
            env[OwinConstants.LocalPort] = "34";
            var headers = new Dictionary<string, string[]>();
            headers["Host"] = new[] { "www.foo.com" };
            env[OwinConstants.RequestHeaders] = headers;
            var request = new ServerRequest(env);

            Assert.Equal("www.foo.com", request.Url.Host);
            Assert.Equal(443, request.Url.Port);
        }

        internal static class OwinConstants
        {
            public const string Version = "owin.Version";

            public const string RequestBody = "owin.RequestBody";
            public const string RequestHeaders = "owin.RequestHeaders";
            public const string RequestScheme = "owin.RequestScheme";
            public const string RequestMethod = "owin.RequestMethod";
            public const string RequestPathBase = "owin.RequestPathBase";
            public const string RequestPath = "owin.RequestPath";
            public const string RequestQueryString = "owin.RequestQueryString";
            public const string RequestProtocol = "owin.RequestProtocol";

            public const string RemoteIpAddress = "server.RemoteIpAddress";
            public const string RemotePort = "server.RemotePort";
            public const string LocalIpAddress = "server.LocalIpAddress";
            public const string LocalPort = "server.LocalPort";

            public const string HostAppNameKey = "host.AppName";
        }
    }
}
