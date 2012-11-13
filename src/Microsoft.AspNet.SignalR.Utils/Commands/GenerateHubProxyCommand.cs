// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.AspNet.SignalR.Utils
{
    internal class GenerateHubProxyCommand : Command
    {
        public GenerateHubProxyCommand(Action<string> info, Action<string> success, Action<string> warning, Action<string> error)
            : base(info, success, warning, error)
        {

        }

        public override string DisplayName
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Notify_GenerateHubProxy); }
        }

        public override string Help
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.Notify_GeneratesHubProxyJSFilesForHub); }
        }

        public override string[] Names
        {
            get { return new[] { "ghp" }; }
        }

        public override void Execute(string[] args)
        {
            bool absolute = false;
            string path = null;
            string outputPath = null;
            string url = null;

            ParseArguments(args, out url, out absolute, out path, out outputPath);

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
                OutputHubsFromUrl(url, outputPath, absolute);
            }
            else
            {
                OutputHubs(path, url, outputPath);
            }
        }

        private void OutputHubs(string path, string url, string outputPath)
        {
            path = path ?? Directory.GetCurrentDirectory();
            url = url ?? "/signalr";

            var assemblies = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Info(String.Format(CultureInfo.CurrentCulture, Resources.Notify_CreatingTempDirectory, tempPath));

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

            var generator = (JavaScriptGenerator)domain.CreateInstanceAndUnwrap(typeof(Program).Assembly.FullName,
                                                                                typeof(JavaScriptGenerator).FullName);
            var js = generator.GenerateProxy(path, url);

            Generate(outputPath, js);
        }

        private static void Copy(string sourcePath, string destinationPath)
        {
            string target = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, target, overwrite: true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.EndsWith(System.String)", Justification = "All ends with methods are SignalR/networking terms.  Will not change via localization.")]
        private static void OutputHubsFromUrl(string url, string outputPath, bool absolute)
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
            string js;

            using (var wc = new WebClient())
            {
                js = wc.DownloadString(uri);
            }

            if (absolute)
            {
                js = Regex.Replace(js, @"=(\w+)\(""(.*?/signalr)""\)", m =>
                {
                    return "=" + m.Groups[1].Value + "(\"" + baseUrl + "\")";
                });
            }

            Generate(outputPath, js);
        }

        private static void Generate(string outputPath, string js)
        {
            File.WriteAllText(outputPath, js);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.StartsWith(System.String)", Justification = "All starts with methods are SignalR/networking terms.  Will not change via localization.")]
        private static void ParseArguments(string[] args, out string url, out bool absolute, out string path, out string outputPath)
        {
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

        public class JavaScriptGenerator : MarshalByRefObject
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Called from non-static.")]
            public string GenerateProxy(string path, string url)
            {
                foreach (var assemblyPath in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
                {
                    Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath));
                }

                var signalrAssembly = (from a in AppDomain.CurrentDomain.GetAssemblies()
                                       where a.GetName().Name.Equals("Microsoft.AspNet.SignalR.Core", StringComparison.OrdinalIgnoreCase)
                                       select a).FirstOrDefault();

                if (signalrAssembly == null)
                {
                    return null;
                }

                Type resolverType = signalrAssembly.GetType("Microsoft.AspNet.SignalR.DefaultDependencyResolver");
                if (resolverType == null)
                {
                    return null;
                }

                Type proxyGeneratorType = signalrAssembly.GetType("Microsoft.AspNet.SignalR.Hubs.DefaultJavaScriptProxyGenerator");
                if (proxyGeneratorType == null)
                {
                    return null;
                }

                object resolver = Activator.CreateInstance(resolverType);
                dynamic proxyGenerator = Activator.CreateInstance(proxyGeneratorType, resolver);

                return proxyGenerator.GenerateProxy(url, true);
            }
        }
    }
}
