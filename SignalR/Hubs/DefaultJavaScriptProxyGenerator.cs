using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultJavaScriptProxyGenerator : IJavaScriptProxyGenerator
    {
        private static readonly Lazy<string> _template = new Lazy<string>(GetTemplate);
        private static readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Type[] _numberTypes = new[] { typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(decimal), typeof(double) };
        private static readonly Type[] _dateTypes = new[] { typeof(DateTime), typeof(DateTimeOffset) };

        private const string ScriptResource = "SignalR.Scripts.hubs.js";

        private readonly IHubManager _manager;
        private readonly IJavaScriptMinifier _javascriptMinifier;

        public DefaultJavaScriptProxyGenerator(IDependencyResolver resolver) :
            this(resolver.Resolve<IHubManager>(),
                 resolver.Resolve<IJavaScriptMinifier>())
        {
        }

        public DefaultJavaScriptProxyGenerator(IHubManager manager, IJavaScriptMinifier javascriptMinifier)
        {
            _manager = manager;
            _javascriptMinifier = javascriptMinifier ?? NullJavaScriptMinifier.Instance;
        }

        public bool IsDebuggingEnabled { get; set; }

        public string GenerateProxy(string serviceUrl, bool includeDocComments = false)
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
                script = _javascriptMinifier.Minify(script);
            }

            _scriptCache.TryAdd(serviceUrl, script);

            return script;
        }

        private void GenerateType(StringBuilder sb, HubDescriptor descriptor, bool includeDocComments)
        {
            // Get only actions with minimum number of parameters.
            var methods = GetMethods(descriptor);

            var members = methods.Select(m => m.Name).OrderBy(name => name).ToList();

            sb.AppendFormat("signalR.{0} = {{", GetHubName(descriptor)).AppendLine();
            sb.AppendFormat("        _: {{").AppendLine();
            sb.AppendFormat("            hubName: '{0}',", descriptor.Name ?? "null").AppendLine();
            sb.AppendFormat("            ignoreMembers: [{0}],", Commas(members, m => "'" + Json.CamelCase(m) + "'")).AppendLine();
            sb.AppendLine("            connection: function () { return signalR.hub; }");
            sb.AppendFormat("        }}");

            if (methods.Any())
            {
                sb.Append(",").AppendLine();
            }

            bool first = true;

            foreach (var method in methods)
            {
                if (!first)
                {
                    sb.Append(",").AppendLine();
                }
                this.GenerateMethod(sb, method, includeDocComments);
                first = false;
            }
            sb.AppendLine();
            sb.Append("    }");
        }

        protected virtual string GetHubName(HubDescriptor descriptor)
        {
            return Json.CamelCase(descriptor.Name);
        }

        private IEnumerable<MethodDescriptor> GetMethods(HubDescriptor descriptor)
        {
            return from method in _manager.GetHubMethods(descriptor.Name)
                   group method by method.Name into overloads
                   let oload = (from overload in overloads
                                orderby overload.Parameters.Count
                                select overload).FirstOrDefault()
                   select oload;
        }

        private void GenerateMethod(StringBuilder sb, MethodDescriptor method, bool includeDocComments)
        {
            var parameterNames = method.Parameters.Select(p => p.Name).ToList();
            sb.AppendLine();
            sb.AppendFormat("        {0}: function ({1}) {{", GetMethodName(method), Commas(parameterNames)).AppendLine();
            if (includeDocComments)
            {
                sb.AppendFormat("            /// <summary>Calls the {0} method on the server-side {1} hub.&#10;Returns a jQuery.Deferred() promise.</summary>", method.Name, method.Hub.Name).AppendLine();
                var parameterDoc = method.Parameters.Select(p => String.Format("            /// <param name=\"{0}\" type=\"{1}\">Server side type is {2}</param>", p.Name, MapToJavaScriptType(p.Type), p.Type)).ToList();
                if (parameterDoc.Any())
                {
                    sb.AppendLine(String.Join(Environment.NewLine, parameterDoc));
                }
            }
            sb.AppendFormat("            return invoke(this, \"{0}\", $.makeArray(arguments));", method.Name).AppendLine();
            sb.Append("        }");
        }

        private string MapToJavaScriptType(Type type)
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

        private static string GetMethodName(MethodDescriptor method)
        {
            return Json.CamelCase(method.Name);
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
                using (var reader = new StreamReader(resourceStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}