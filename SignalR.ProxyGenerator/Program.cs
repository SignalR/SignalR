using System;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Ajax.Utilities;

namespace SignalR.ProxyGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            bool minify = false;
            bool absolute = false;

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: {0} [url] (/minify) (/absolute)", typeof(Program).Assembly.GetName().Name);                
                return;
            }

            ParseArguments(args, out minify, out absolute);

            string url = args[0];
            string baseUrl = null;
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            if(!url.EndsWith("signalr"))
            {
                url += "signalr/";
            }

            baseUrl = url;
            if (!url.EndsWith("hubs", StringComparison.OrdinalIgnoreCase))
            {
                url += "hubs";
            }

            var uri = new Uri(url);

            var minifier = new Minifier();
            var wc = new WebClient();
            string js = wc.DownloadString(uri);
            if (absolute)
            {
                js = Regex.Replace(js, @"\(""(.*?/signalr)""\)", m => "(\"" + baseUrl + "\")");
            }

            if (minify)
            {
                Console.WriteLine(minifier.MinifyJavaScript(js));
            }
            else
            {
                Console.WriteLine(js);
            }
        }

        private static void ParseArguments(string[] args, out bool minify, out bool absolute)
        {
            minify = false;
            absolute = false;
            foreach (var a in args)
            {
                if (!a.StartsWith("/"))
                {
                    continue;
                }

                var arg = a.Substring(1).ToLowerInvariant();
                switch (arg)
                {
                    case "minify":
                        minify = true;
                        break;
                    case "absolute":
                        absolute = true;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
