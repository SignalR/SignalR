using System;
using System.Net;
using Microsoft.Ajax.Utilities;

namespace SignalR.ProxyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: {0} [url]", typeof(Program).Assembly.GetName().Name);
                return;
            }

            string url = args[0];
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            if (!url.EndsWith("hubs", StringComparison.OrdinalIgnoreCase))
            {
                url += "hubs";
            }

            var uri = new Uri(url);

            var minifier = new Minifier();
            var wc = new WebClient();
            Console.WriteLine(minifier.MinifyJavaScript(wc.DownloadString(uri)));
        }
    }
}
