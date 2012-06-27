using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
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
            string path = null;
            string outputPath = null;
            string url = null;
            string signalrAmdAlias = "signalr";
            bool amd = true;

            ParseArguments(args, out url, out minify, out absolute, out path, out outputPath, out amd, out signalrAmdAlias);

            if (String.IsNullOrEmpty(outputPath))
            {
                outputPath = ".";
            }

            outputPath = Path.GetFullPath(outputPath);

            if (String.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
            {
                outputPath = Path.Combine(outputPath, "server.js");
            }

            if (!String.IsNullOrEmpty(url) && String.IsNullOrEmpty(path))
            {
                OutputHubsFromUrl(url, outputPath, minify, absolute, amd, signalrAmdAlias);
            }
            else
            {
                OutputHubs(path, url, outputPath, minify, amd, signalrAmdAlias);
            }
        }

        private static void OutputHubs(string path, string url, string outputPath, bool minify, bool amd, string signalrAmdAlias)
        {
            path = path ?? Directory.GetCurrentDirectory();
            url = url ?? "/signalr";

            var assemblies = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Console.WriteLine("Creating temp directory {0}", tempPath);

            Directory.CreateDirectory(tempPath);

            // Copy all assemblies to temp
            foreach (var assemblyPath in assemblies)
            {
                Copy(assemblyPath, tempPath);
            }

            Copy(typeof(Program).Assembly.Location, tempPath);

            var setup = new AppDomainSetup
            {
                ApplicationBase = tempPath
            };

            var domain = AppDomain.CreateDomain("hubs", AppDomain.CurrentDomain.Evidence, setup);

            var minifier = new Minifier();
            var generator = (JavascriptGenerator)domain.CreateInstanceAndUnwrap(typeof(Program).Assembly.FullName,
                                                                                typeof(JavascriptGenerator).FullName);
            var js = generator.GenerateProxy(path, url);

            Generate(outputPath, minify, minifier, js, amd, signalrAmdAlias);
        }

        private static void Copy(string sourcePath, string destinationPath)
        {
            string target = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, target, overwrite: true);

            Console.WriteLine("Copied file {0} to {1}", Path.GetFullPath(sourcePath), destinationPath);
        }

        private static void OutputHubsFromUrl(string url, string outputPath, bool minify, bool absolute, bool amd, string signalrAmdAlias)
        {
            string baseUrl = null;
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            if (!url.EndsWith("signalr"))
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
                js = Regex.Replace(js, @"=(\w+)\(""(.*?/signalr)""\)", m =>
                {
                    return "=" + m.Groups[1].Value + "(\"" + baseUrl + "\")";
                });
            }

            Generate(outputPath, minify, minifier, js, amd, signalrAmdAlias);
        }

        private static void Generate(string outputPath, bool minify, Minifier minifier, string js, bool amd, string signalrAmdAlias)
        {
            var jsText = js;

            if (amd)
            {
                jsText = WrapWithAmdDefinition(js, signalrAmdAlias);
            }

            if (minify)
            {
                File.WriteAllText(outputPath, minifier.MinifyJavaScript(jsText));
            }
            else
            {
                File.WriteAllText(outputPath, jsText);
            }
        }

        /// <summary>
        /// Adds AMD compliant define wrapper at top and bottom of output hubs file 
        /// for inclusion in RequreJS-ified client scripts.
        /// 
        /// Usage for default AMD alias of'signalr' -> hubify.exe /o:[path_to_output_file]\hubs.js /amd 
        /// Usage for custom AMD alias -> hubify.exe /o:[path_to_output_file]\hubs.js /amd:[aliasName] 
        /// </summary>
        private static string WrapWithAmdDefinition(string js, string signalrAmdAlias)
        {
            var amdWrappedJs = new StringBuilder();

            amdWrappedJs.AppendLine(string.Format("define(['jquery', '{0}'], function () {{", signalrAmdAlias));
            amdWrappedJs.Append(js);
            amdWrappedJs.AppendLine("});");

            return amdWrappedJs.ToString();
        }

        private static void ParseArguments(string[] args, out string url, out bool minify, out bool absolute, out string path, out string outputPath, out bool amd, out string signalrAmdAlias)
        {
            minify = false;
            amd = false;
            signalrAmdAlias = "signalr";
            absolute = false;
            path = null;
            url = null;
            outputPath = null;

            foreach (var a in args)
            {
                if (!a.StartsWith("/"))
                {
                    continue;
                }

                KeyValuePair<string, string> arg = ParseArg(a);
                switch (arg.Key)
                {
                    case "minify":
                        minify = true;
                        break;
                    case "amd":
                        amd = true;
                        if (!string.IsNullOrWhiteSpace(arg.Value)) signalrAmdAlias = arg.Value;
                        break;
                    case "absolute":
                        absolute = true;
                        break;
                    case "path":
                        path = arg.Value;
                        break;
                    case "url":
                        url = arg.Value;
                        break;
                    case "o":
                        outputPath = arg.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        private static KeyValuePair<string, string> ParseArg(string arg)
        {
            arg = arg.Substring(1);
            if (arg.Contains(":"))
            {
                var splitIndex = arg.IndexOf(':');
                var key = arg.Substring(0, splitIndex).Trim();
                var value = arg.Substring(splitIndex + 1).Trim();
                return new KeyValuePair<string, string>(key, value);
            }

            return new KeyValuePair<string, string>(arg.Trim(), null);
        }

        public class JavascriptGenerator : MarshalByRefObject
        {
            public string GenerateProxy(string path, string url)
            {
                foreach (var assemblyPath in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
                {
                    Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath));
                }

                var signalrAssembly = (from a in AppDomain.CurrentDomain.GetAssemblies()
                                       where a.GetName().Name.Equals("SignalR", StringComparison.OrdinalIgnoreCase)
                                       select a).FirstOrDefault();

                if (signalrAssembly == null)
                {
                    return null;
                }

                Type resolverType = signalrAssembly.GetType("SignalR.DefaultDependencyResolver");
                if (resolverType == null)
                {
                    return null;
                }

                Type proxyGeneratorType = signalrAssembly.GetType("SignalR.Hubs.DefaultJavaScriptProxyGenerator");
                if (proxyGeneratorType == null)
                {
                    return null;
                }

                object resolver = Activator.CreateInstance(resolverType);
                dynamic proxyGenerator = Activator.CreateInstance(proxyGeneratorType, resolver);

                return proxyGenerator.GenerateProxy(url);
            }
        }
    }
}