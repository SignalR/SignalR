using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gate;
using Gate.Middleware;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub;
using Microsoft.AspNet.SignalR.Samples.Raw;
using Microsoft.AspNet.SignalR.Samples.Streaming;
using Owin;

namespace Microsoft.AspNet.SignalR.Hosting.Console.Samples
{
    public static class Startup
    {
        public static void Configuration(IAppBuilder app)
        {
            var staticFileBasePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                @"..\..\..\SignalR.Hosting.AspNet.Samples");

            Directory.SetCurrentDirectory(staticFileBasePath);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var context = GlobalHost.ConnectionManager.GetConnectionContext<StreamingConnection>();
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DemoHub>();

                while (true)
                {
                    try
                    {
                        context.Connection.Broadcast(DateTime.Now.ToString());
                        hubContext.Clients.All.fromArbitraryCode(DateTime.Now.ToString());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("SignalR error thrown in Streaming broadcast: {0}", ex);
                    }
                    Thread.Sleep(2000);
                }
            });

            app.MapHubs("/signalr");

            app.MapConnection<SendingConnection>("/sending-connection");
            app.MapConnection<TestConnection>("/test-connection");
            app.MapConnection<RawConnection>("/raw/raw");
            app.MapConnection<StreamingConnection>("/streaming/streaming");

            app.UseType<RedirectFoldersWithoutSlashes>();
            app.UseType<DefaultStaticFileName>("index.htm");
            app.UseType<DefaultStaticFileName>("index.html");
            app.UseType<ExtensionContentType>(".htm", "text/plain");
            app.UseType<ExtensionContentType>(".html", "text/plain");
            app.UseStatic(staticFileBasePath);
        }

        public class RedirectFoldersWithoutSlashes
        {
            private readonly Func<IDictionary<string, object>, Task> _next;

            public RedirectFoldersWithoutSlashes(Func<IDictionary<string, object>, Task> next)
            {
                _next = next;
            }

            public Task Invoke(IDictionary<string, object> env)
            {
                var req = new Request(env);
                if (req.Path.StartsWith("/") && !req.Path.EndsWith("/") && Directory.Exists(req.Path.Substring(1)))
                {
                    var resp = new Response(env)
                    {
                        StatusCode = 301
                    };
                    resp.Headers["Location"] = new[] { req.PathBase + req.Path + "/" };
                    return TaskAsyncHelper.Empty;
                }

                return _next(env);                
            }
        }

        public class ExtensionContentType
        {
            private readonly Func<IDictionary<string, object>, Task> _next;
            private readonly string _extension;
            private readonly string _contentType;

            public ExtensionContentType(Func<IDictionary<string, object>, Task> next, string extension, string contentType)
            {
                _next = next;
                _extension = extension;
                _contentType = contentType;
            }

            public Task Invoke(IDictionary<string, object> env)
            {
                var req = new Request(env);
                var res = new Response(env);
                if (String.IsNullOrEmpty(res.ContentType) &&
                    req.Path.EndsWith(_extension, StringComparison.OrdinalIgnoreCase))
                {
                    res.ContentType = _contentType;
                }
                return _next(env);
            }
        }

        public class DefaultStaticFileName
        {
            private readonly Func<IDictionary<string, object>, Task> _next;
            private readonly string _fileName;

            public DefaultStaticFileName(Func<IDictionary<string, object>, Task> next, string fileName)
            {
                _next = next;
                _fileName = fileName;
            }

            public Task Invoke(IDictionary<string, object> env)
            {
                var req = new Request(env);
                if (req.Path.EndsWith("/") && File.Exists(Path.Combine(req.Path.Substring(1), _fileName)))
                {
                    req.Path += _fileName;
                }
                return _next(env);
            }
        }
    }
}