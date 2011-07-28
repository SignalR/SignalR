using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using Microsoft.Ajax.Utilities;

namespace SignalR.Hubs {
    public class DefaultJavaScriptProxyGenerator : IJavaScriptProxyGenerator {
        private static readonly Lazy<string> _template = new Lazy<string>(GetTemplate);
        private static readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private const string ScriptResource = "SignalR.Scripts.hubs.js";

        private readonly IHubLocator _hubLocator;

        public DefaultJavaScriptProxyGenerator(IHubLocator hubLocator) {
            _hubLocator = hubLocator;
        }

        public string GenerateProxy(HttpContextBase context, string serviceUrl) {
            string script;
            if (_scriptCache.TryGetValue(serviceUrl, out script)) {
                return script;
            }

            var template = _template.Value;

            script = template.Replace("{serviceUrl}", serviceUrl);

            var hubs = new StringBuilder();
            var first = true;
            foreach (var type in _hubLocator.GetHubs()) {
                if (!first) {
                    hubs.AppendLine(",");
                    hubs.Append("        ");
                }
                GenerateType(serviceUrl, hubs, type);
                first = false;
            }

            script = script.Replace("/*hubs*/", hubs.ToString());

            if (!context.IsDebuggingEnabled) {
                var minifier = new Minifier();
                script = minifier.MinifyJavaScript(script);
            }

            _scriptCache.TryAdd(serviceUrl, script);

            return script;
        }

        private void GenerateType(string serviceUrl, StringBuilder sb, Type type) {
            // Get public instance methods declared on this type only
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var members = methods.Select(m => m.Name).ToList();
            members.Add("namespace");
            members.Add("serverMembers");
            members.Add("callbacks");

            sb.AppendFormat("{0}: {{", Json.CamelCase(type.Name)).AppendLine();
            sb.AppendFormat("            _: {{").AppendLine();
            sb.AppendFormat("                hubName: '{0}',", type.FullName ?? "null").AppendLine();
            sb.AppendFormat("                serverMembers: [{0}],", Commas(members, m => "'" + Json.CamelCase(m) + "'")).AppendLine();
            sb.AppendLine("                connection: function () { return window.signalR.hub; }");
            sb.AppendFormat("            }},").AppendLine();
            sb.AppendFormat("            state: {{}}");
            if (methods.Any()) {
                sb.Append(",");
            }
            bool first = true;

            var propertyMethods = new HashSet<MethodInfo>();
            foreach (var property in type.GetProperties()) {
                propertyMethods.Add(property.GetGetMethod());
                propertyMethods.Add(property.GetSetMethod());
            }

            foreach (var method in methods) {
                if (propertyMethods.Contains(method)) {
                    continue;
                }

                if (!first) {
                    sb.Append(",").AppendLine();
                }
                GenerateMethod(serviceUrl, sb, type, method);
                first = false;
            }
            sb.AppendLine();
            sb.Append("        }");
        }

        private void GenerateMethod(string serviceUrl, StringBuilder sb, Type type, MethodInfo method) {
            var parameters = method.GetParameters();
            var parameterNames = parameters.Select(p => p.Name).ToList();
            parameterNames.Add("callback");
            sb.AppendLine();
            sb.AppendFormat("            {0}: function ({1}) {{", Json.CamelCase(method.Name), Commas(parameterNames)).AppendLine();
            sb.AppendFormat("                return serverCall(this, \"{0}\", $.makeArray(arguments));", method.Name).AppendLine();
            sb.Append("            }");
        }

        private static string Commas(IEnumerable<string> values) {
            return Commas(values, v => v);
        }

        private static string Commas<T>(IEnumerable<T> values, Func<T, string> selector) {
            return String.Join(", ", values.Select(selector));
        }

        private static string GetTemplate() {
            using (Stream resourceStream = typeof(DefaultJavaScriptProxyGenerator).Assembly.GetManifestResourceStream(ScriptResource)) {
                using (var reader = new StreamReader(resourceStream)) {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
