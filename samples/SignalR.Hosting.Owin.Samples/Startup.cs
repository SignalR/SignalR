using System;
using System.IO;
using Gate;
using Gate.Middleware;
using Owin;
using SignalR.Samples.Raw;

namespace SignalR.Hosting.Owin.Samples
{
    public class Startup
    {
        public static void Configuration(IAppBuilder builder)
        {
            var applicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var contentFolder = Path.Combine(applicationBase, "Content");

            builder
                .Use(LogToConsole)
                .UseShowExceptions()
                .UseSignalRHubs()
                .UseSignalR<Raw>("/Raw/Connection")
                .Use(Alias, "/", "/index.html")
                .UseStatic(contentFolder);
        }

        public static AppDelegate Alias(AppDelegate app, string path, string alias)
        {
            return
                (env, result, fault) =>
                {
                    var req = new Request(env);
                    if (req.Path == path)
                    {
                        req.Path = alias;
                    }
                    app(env, result, fault);
                };
        }

        public static AppDelegate LogToConsole(AppDelegate app)
        {
            return
                (env, result, fault) =>
                {
                    var req = new Request(env);
                    Console.WriteLine(req.Method + " " + req.PathBase + req.Path);
                    app(env, result, fault);
                };
        }
    }
}