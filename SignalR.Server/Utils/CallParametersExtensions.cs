﻿using Owin;

namespace SignalR.Server.Utils
{
    static class CallParametersExtensions
    {
        public static T Get<T>(this CallParameters call, string key) where T : class
        {
            object value;
            return call.Environment.TryGetValue(key, out value) ? value as T : null;
        }

        public static string Scheme(this CallParameters call)
        {
            return call.Get<string>("owin.RequestScheme");
        }

        public static string PathBase(this CallParameters call)
        {
            return call.Get<string>("owin.RequestPathBase");
        }

        public static string Path(this CallParameters call)
        {
            return call.Get<string>("owin.RequestPath");
        }

        public static string QueryString(this CallParameters call)
        {
            return call.Get<string>("owin.RequestQueryString");
        }

        public static string HostAndPort(this CallParameters call)
        {
            var host = call.GetHeader("Host");
            if (host != null)
            {
                return host;
            }
            var localIp = call.LocalIp() ?? "127.0.0.1";
            var localPort = call.LocalPort();
            return localPort != null ? localIp + ":" + localPort : localIp;
        }

        public static string Host(this CallParameters call)
        {
            var host = call.GetHeader("Host");
            if (host != null)
            {
                var delimiter = host.LastIndexOf(':');
                return delimiter == -1 ? host : host.Substring(0, delimiter);
            }
            return call.LocalIp() ?? "127.0.0.1";
        }

        public static int Port(this CallParameters call)
        {
            int port;
            var host = call.GetHeader("Host");
            if (host != null)
            {
                var delimiter = host.LastIndexOf(':');
                if (delimiter != -1)
                {
                    if (int.TryParse(host.Substring(delimiter + 1), out port))
                    {
                        return port;
                    }
                }
            }
            var localPort = call.LocalPort();
            if (localPort != null && int.TryParse(localPort, out port))
            {
                return port;
            }
            return call.Scheme() == "https" ? 433 : 80;
        }

        public static string GetHeader(this CallParameters call, string name)
        {
            string[] values;
            if (call.Headers == null ||
                !call.Headers.TryGetValue(name, out values) ||
                values == null ||
                values.Length == 0)
            {
                return null;
            }

            return values.Length == 1 ? values[0] : string.Join(",", values);
        }

        public static string LocalIp(this CallParameters call)
        {
            return call.Get<string>("server.LocalIp");
        }
        public static string LocalPort(this CallParameters call)
        {
            return call.Get<string>("server.LocalPort");
        }
        public static string RemoteIp(this CallParameters call)
        {
            return call.Get<string>("server.RemoteIp");
        }
    }
}
