using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#if WINDOWS_PHONE
using System.Windows;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Hubs {
    public class HubConnection : Connection {
        private readonly Dictionary<string, Tuple<Type, MethodInfo>> _actionMap = new Dictionary<string, Tuple<Type, MethodInfo>>(StringComparer.OrdinalIgnoreCase);
        private readonly Lazy<IEnumerable<string>> _actions;

        public HubConnection(string baseUrl)
            : base(baseUrl) {
            _actions = new Lazy<IEnumerable<string>>(GetActions);
        }

        public Func<Type, object> ObjectFactory { get; set; }

        public override Task Start() {
            Sending += OnSending;
            Received += OnReceived;
            return base.Start();
        }

        public override void Stop() {
            Sending -= OnSending;
            Received -= OnReceived;
            base.Stop();
        }

        public IHubProxy CreateProxy(string hub) {
            return new HubProxy(this, hub);
        }

        private string OnSending() {
            return JsonConvert.SerializeObject(_actions.Value);
        }

        private void OnReceived(string message) {
            var info = JsonConvert.DeserializeObject<HubInvocationInfo>(message);
            Tuple<Type, MethodInfo> mapping;
            if (_actionMap.TryGetValue(info.Action, out mapping)) {
                Type hubType = mapping.Item1;
                MethodInfo method = mapping.Item2;

                ObjectFactory = ObjectFactory ?? Activator.CreateInstance;

                var hub = ObjectFactory(hubType);
                method.Invoke(hub, ResolveParameters(method, info.Args));
            }
        }

        private object[] ResolveParameters(MethodInfo method, object[] args) {
            return (from p in method.GetParameters()
                    orderby p.Position
                    select ChangeType(args[p.Position], p.ParameterType)).ToArray();
        }

        private object ChangeType(object value, Type type) {
            var jsonObject = value as JObject;
            if (jsonObject != null) {
                if (type == typeof(object)) {
                    return jsonObject;
                }
                else {
                    return JsonConvert.DeserializeObject(jsonObject.ToString(), type);
                }
            }
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }


        private IEnumerable<string> GetActions() {
#if !WINDOWS_PHONE
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
#else
            var parts = Deployment.Current.Parts;
            foreach (var part in parts) {
                var assemblyName = part.Source.Replace(".dll", String.Empty);
                var a = Assembly.Load(assemblyName);
#endif
                foreach (var action in GetActions(a)) {
                    yield return action;
                }
            }
        }

        private IEnumerable<string> GetActions(Assembly assembly) {
            foreach (var type in assembly.GetTypes()) {
                var attr = (HubAttribute)type.GetCustomAttributes(typeof(HubAttribute), true).FirstOrDefault();
                if (attr == null) {
                    continue;
                }

                foreach (var method in type.GetMethods()) {
                    string action = attr.Type + "." + GetMessage(method);
                    _actionMap[action] = Tuple.Create(type, method);
                    yield return action;
                }
            }
        }

        private string GetMessage(MethodInfo method) {
            var attr = (HubActionAttribute)method.GetCustomAttributes(typeof(HubActionAttribute), true).FirstOrDefault();
            if (attr == null) {
                return method.Name;
            }
            return attr.Message;
        }

        public class HubInvocationInfo {
            public string Action { get; set; }
            public object[] Args { get; set; }
        }

#if WINDOWS_PHONE
        private class Lazy<T> where T : class {
            private readonly Func<T> _factory;
            private T _value;
            private readonly object _lock = new object();
            public Lazy(Func<T> factory) {
                _factory = factory;
            }

            public T Value {
                get {
                    if (_value == null) {
                        lock (_lock) {
                            if (_value == null) {
                                _value = _factory();
                            }
                        }
                    }
                    return _value;
                }
            }
        }
#endif
        private static string GetUrl(string baseUrl) {
            if (!baseUrl.EndsWith("/")) {
                baseUrl += "/";
            }
            return baseUrl + "signalr";
        }
    }
}
