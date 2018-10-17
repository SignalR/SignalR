// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

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

        public override int Execute(string[] args)
        {
            ParseArguments(args, out var url, out var path, out var outputPath, out var configFile);

            if (String.IsNullOrEmpty(outputPath))
            {
                outputPath = ".";
            }

            outputPath = Path.GetFullPath(outputPath);

            if (String.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
            {
                outputPath = Path.Combine(outputPath, "server.js");
            }

            return OutputHubs(path, url, outputPath, configFile) ? 0 : 1;
        }

        private bool OutputHubs(string path, string url, string outputPath, string configFile)
        {
            path = path ?? Directory.GetCurrentDirectory();
            url = url ?? "/signalr";

            var assemblies = Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories)
                      .Concat(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));

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

            if (!string.IsNullOrEmpty(configFile))
            {
                setup.ConfigurationFile = configFile;
            }

            string js = null;
            try
            {
                var domain = AppDomain.CreateDomain("hubs", AppDomain.CurrentDomain.Evidence, setup);

                var generator = (JavaScriptGenerator)domain.CreateInstanceAndUnwrap(typeof(Program).Assembly.FullName,
                                                                                    typeof(JavaScriptGenerator).FullName);
                js = generator.GenerateProxy(path, url, Warning);
            }
            catch (TargetInvocationException tie) when (tie.InnerException is FileLoadException fle)
            {
                // Missing binding redirect :(
                Console.Error.WriteLine(string.Format(Resources.Error_MissingBindingRedirect, fle.FileName));
                return false;
            }

            Generate(outputPath, js);
            return true;
        }

        private static void Copy(string sourcePath, string destinationPath)
        {
            string target = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, target, overwrite: true);
        }

        private static void Generate(string outputPath, string js)
        {
            File.WriteAllText(outputPath, js);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.StartsWith(System.String)", Justification = "All starts with methods are SignalR/networking terms.  Will not change via localization.")]
        private static void ParseArguments(string[] args, out string url, out string path, out string outputPath, out string configFile)
        {
            path = null;
            url = null;
            outputPath = null;
            configFile = null;

            foreach (var a in args)
            {
                if (!a.StartsWith("/"))
                {
                    continue;
                }

                KeyValuePair<string, string> arg = ParseArg(a);
                switch (arg.Key)
                {
                    case "path":
                        path = arg.Value;
                        break;
                    case "url":
                        url = arg.Value;
                        break;
                    case "configFile":
                        configFile = arg.Value;
                        break;
                    case "o":
                        outputPath = arg.Value;
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
            public string GenerateProxy(string path, string url, Action<string> warning)
            {
                var assemblies = Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories)
                          .Concat(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));

                foreach (var assemblyPath in assemblies)
                {
                    try
                    {
                        Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath));
                    }
                    catch (BadImageFormatException e)
                    {
                        warning(e.Message);
                    }
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
