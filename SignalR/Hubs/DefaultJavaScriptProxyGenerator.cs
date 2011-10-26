using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultJavaScriptProxyGenerator : IJavaScriptProxyGenerator
    {
        private static readonly Lazy<string> _template = new Lazy<string>(GetTemplate);
        private static readonly ConcurrentDictionary<string, string> _scriptCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private const string ScriptResource = "SignalR.Scripts.hubs.js";

        private readonly IHubLocator _hubLocator;
        private readonly IJavaScriptMinifier _javascriptMinifier;

        public DefaultJavaScriptProxyGenerator(IHubLocator hubLocator, IJavaScriptMinifier javascriptMinifier)
        {
            _hubLocator = hubLocator;
            _javascriptMinifier = javascriptMinifier;
        }

        public string GenerateProxy(HttpContextBase context, string serviceUrl)
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
            foreach (var type in _hubLocator.GetHubs())
            {
                if (!first)
                {
                    hubs.AppendLine(",");
                    hubs.Append("        ");
                }
                GenerateType(serviceUrl, hubs, type);
                first = false;
            }

            script = script.Replace("/*hubs*/", hubs.ToString());

            if (!context.IsDebuggingEnabled)
            {
                script = _javascriptMinifier.Minify(script);
            }

            _scriptCache.TryAdd(serviceUrl, script);

            return script;
        }

        private void GenerateType(string serviceUrl, StringBuilder sb, Type type)
        {
            // Get public instance methods declared on this type only
            var methods = GetMethods(type);
            var members = methods.Select(m => m.Name).ToList();
            members.Add("namespace");
            members.Add("ignoreMembers");
            members.Add("callbacks");

            sb.AppendFormat("{0}: {{", GetHubName(type)).AppendLine();
            sb.AppendFormat("            _: {{").AppendLine();
            sb.AppendFormat("                hubName: '{0}',", type.FullName ?? "null").AppendLine();
            sb.AppendFormat("                ignoreMembers: [{0}],", Commas(members, m => "'" + Json.CamelCase(m) + "'")).AppendLine();
            sb.AppendLine("                connection: function () { return signalR.hub; }");
            sb.AppendFormat("            }}").AppendLine();
            if (methods.Any())
            {
                sb.Append(",");
            }
            bool first = true;

            foreach (var method in methods)
            {
                if (!first)
                {
                    sb.Append(",").AppendLine();
                }
                GenerateMethod(serviceUrl, sb, type, method);
                first = false;
            }
            sb.AppendLine();
            sb.Append("        }");
        }

        protected virtual string GetHubName(Type type)
        {
            return GetAttributeValue<HubNameAttribute, string>(type, a => a.HubName) ?? Json.CamelCase(type.Name);
        }

        protected virtual IEnumerable<MethodInfo> GetMethods(Type type)
        {
            return ReflectionHelper.GetExportedHubMethods(type);
        }

        private void GenerateMethod(string serviceUrl, StringBuilder sb, Type type, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var parameterNames = parameters.Select(p => p.Name).ToList();
            parameterNames.Add("callback");
            sb.AppendLine();
            sb.AppendFormat("            {0}: function ({1}) {{", GetMethodName(method), Commas(parameterNames)).AppendLine();
            sb.AppendFormat("                return serverCall(this, \"{0}\", $.makeArray(arguments));", method.Name).AppendLine();
            sb.Append("            }");
        }

        private static string GetMethodName(MethodInfo method)
        {
            return GetAttributeValue<HubMethodNameAttribute, string>(method, a => a.MethodName) ?? Json.CamelCase(method.Name);
        }

        private static TResult GetAttributeValue<TAttribute, TResult>(ICustomAttributeProvider source, Func<TAttribute, TResult> valueGetter)
            where TAttribute : Attribute
        {
            var attributes = source.GetCustomAttributes(typeof(TAttribute), false)
                .Cast<TAttribute>()
                .ToList();
            if (attributes.Any())
            {
                return valueGetter(attributes[0]);
            }
            return default(TResult);
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