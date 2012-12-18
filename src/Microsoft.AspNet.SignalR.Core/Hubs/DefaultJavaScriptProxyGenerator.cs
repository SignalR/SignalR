// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class DefaultJavaScriptProxyGenerator : IJavaScriptProxyGenerator
    {
        private static readonly Lazy<string> _template = new Lazy<string>(GetTemplate);
        private static readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Type[] _numberTypes = new[] { typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(decimal), typeof(double) };
        private static readonly Type[] _dateTypes = new[] { typeof(DateTime), typeof(DateTimeOffset) };

        private const string ScriptResource = "Microsoft.AspNet.SignalR.Scripts.hubs.js";

        private readonly IHubManager _manager;
        private readonly IJavaScriptMinifier _javaScriptMinifier;

        public DefaultJavaScriptProxyGenerator(IDependencyResolver resolver) :
            this(resolver.Resolve<IHubManager>(),
                 resolver.Resolve<IJavaScriptMinifier>())
        {
        }

        public DefaultJavaScriptProxyGenerator(IHubManager manager, IJavaScriptMinifier javaScriptMinifier)
        {
            _manager = manager;
            _javaScriptMinifier = javaScriptMinifier ?? NullJavaScriptMinifier.Instance;
        }

        public bool IsDebuggingEnabled { get; set; }

        public string GenerateProxy(string serviceUrl, bool includeDocComments)
        {
            string script;
            if (_scriptCache.TryGetValue(serviceUrl, out script))
            {
                return script;
            }

            var template = _template.Value;

            script = template.Replace("{serviceUrl}", serviceUrl);

            var hubs = new StringBuilder();
            var first = true;
            foreach (var descriptor in _manager.GetHubs().OrderBy(h => h.Name))
            {
                if (!first)
                {
                    hubs.AppendLine(";");
                    hubs.AppendLine();
                    hubs.Append("    ");
                }
                GenerateType(hubs, descriptor, includeDocComments);
                first = false;
            }

            if (hubs.Length > 0)
            {
                hubs.Append(";");
            }
            script = script.Replace("/*hubs*/", hubs.ToString());

            if (!IsDebuggingEnabled)
            {
                script = _javaScriptMinifier.Minify(script);
            }

            _scriptCache.TryAdd(serviceUrl, script);

            return script;
        }

        private void GenerateType(StringBuilder sb, HubDescriptor descriptor, bool includeDocComments)
        {
            // Get only actions with minimum number of parameters.
            var methods = GetMethods(descriptor);
            var hubName = GetDescriptorName(descriptor);

            sb.AppendFormat("signalR.{0} = signalR.hub.createHubProxy('{1}'); ", hubName, hubName).AppendLine();
            sb.AppendFormat("    signalR.{0}.client = {{ }};", hubName).AppendLine();
            sb.AppendFormat("    signalR.{0}.server = {{", hubName);

            bool first = true;

            foreach (var method in methods)
            {
                if (!first)
                {
                    sb.Append(",").AppendLine();
                }
                this.GenerateMethod(sb, method, includeDocComments, hubName);
                first = false;
            }
            sb.AppendLine();
            sb.Append("    }");
        }

        protected virtual string GetDescriptorName(Descriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }

            string name = descriptor.Name;

            // If the name was not specified then do not camel case
            if (!descriptor.NameSpecified)
            {
                name = Json.CamelCase(name);
            }

            return name;
        }

        private IEnumerable<MethodDescriptor> GetMethods(HubDescriptor descriptor)
        {
            return from method in _manager.GetHubMethods(descriptor.Name)
                   group method by method.Name into overloads
                   let oload = (from overload in overloads
                                orderby overload.Parameters.Count
                                select overload).FirstOrDefault()
                   orderby oload.Name
                   select oload;
        }

        private void GenerateMethod(StringBuilder sb, MethodDescriptor method, bool includeDocComments, string hubName)
        {
            var parameterNames = method.Parameters.Select(p => p.Name).ToList();
            sb.AppendLine();
            sb.AppendFormat("        {0}: function ({1}) {{", GetDescriptorName(method), Commas(parameterNames)).AppendLine();
            if (includeDocComments)
            {
                sb.AppendFormat(Resources.DynamicComment_CallsMethodOnServerSideDeferredPromise, method.Name, method.Hub.Name).AppendLine();
                var parameterDoc = method.Parameters.Select(p => String.Format(CultureInfo.CurrentCulture, Resources.DynamicComment_ServerSideTypeIs, p.Name, MapToJavaScriptType(p.ParameterType), p.ParameterType)).ToList();
                if (parameterDoc.Any())
                {
                    sb.AppendLine(String.Join(Environment.NewLine, parameterDoc));
                }
            }
            sb.AppendFormat("            return signalR.{0}.invoke.apply(signalR.{0}, $.merge([\"{1}\"], $.makeArray(arguments)));", hubName, method.Name).AppendLine();
            sb.Append("         }");
        }

        private static string MapToJavaScriptType(Type type)
        {
            if (!type.IsPrimitive && !(type == typeof(string)))
            {
                return "Object";
            }
            if (type == typeof(string))
            {
                return "String";
            }
            if (_numberTypes.Contains(type))
            {
                return "Number";
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return "Array";
            }
            if (_dateTypes.Contains(type))
            {
                return "Date";
            }
            return String.Empty;
        }

        private static string Commas(IEnumerable<string> values)
        {
            return Commas(values, v => v);
        }

        private static string Commas<T>(IEnumerable<T> values, Func<T, string> selector)
        {
            return String.Join(", ", values.Select(selector));
        }

        private static string GetTemplate()
        {
            using (Stream resourceStream = typeof(DefaultJavaScriptProxyGenerator).Assembly.GetManifestResourceStream(ScriptResource))
            {
                var reader = new StreamReader(resourceStream);
                return reader.ReadToEnd();
            }
        }
    }
}
