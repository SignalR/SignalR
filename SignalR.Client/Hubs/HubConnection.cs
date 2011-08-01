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
        private readonly Dictionary<string, Dictionary<string, MethodInfo>> _hubs = new Dictionary<string, Dictionary<string, MethodInfo>>(StringComparer.OrdinalIgnoreCase);
        private string _serializedRequestData;

        public HubConnection(string url)
            : base(GetUrl(url)) {
        }

        public Func<Type, object> ObjectFactory { get; set; }

        public override Task Start() {
            if (String.IsNullOrEmpty(_serializedRequestData)) {
                ProcessAssemblies();
            }

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
            return _serializedRequestData;
        }

        private void OnReceived(string message) {
            var invocationInfo = JsonConvert.DeserializeObject<HubInvocationInfo>(message);
            Dictionary<string, MethodInfo> methods;
            if (_hubs.TryGetValue(invocationInfo.Hub, out methods)) {
                MethodInfo method;
                if (methods.TryGetValue(invocationInfo.Method, out method)) {
                    ObjectFactory = ObjectFactory ?? Activator.CreateInstance;

                    var hubInstance = ObjectFactory(method.DeclaringType);
                    method.Invoke(hubInstance, ResolveParameters(method, invocationInfo.Args));
                }
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


        private void ProcessAssemblies() {
#if !WINDOWS_PHONE
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
#else
            var parts = Deployment.Current.Parts;
            foreach (var part in parts) {
                var assemblyName = part.Source.Replace(".dll", String.Empty);
                var a = Assembly.Load(assemblyName);
#endif
                ProcessAssembly(a);
            }

            var requestData = new List<object>();
            foreach (var item in _hubs) {
                requestData.Add(new {
                    Name = item.Key,
                    Methods = item.Value.Keys
                });
            }

            // Store the serialized version of this data since it never changes per app domain
            _serializedRequestData = JsonConvert.SerializeObject(requestData);
        }

        private void ProcessAssembly(Assembly assembly) {
            foreach (var type in assembly.GetTypes()) {
                var attr = (HubAttribute)type.GetCustomAttributes(typeof(HubAttribute), true).FirstOrDefault();
                if (attr == null) {
                    continue;
                }

                Dictionary<string, MethodInfo> methods;
                if (!_hubs.TryGetValue(attr.Type, out methods)) {
                    methods = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
                    _hubs[attr.Type] = methods;
                }

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                    var methodAttr = (HubMethodAttribute)method.GetCustomAttributes(typeof(HubMethodAttribute), true).FirstOrDefault();
                    string name = method.Name;
                    if (methodAttr != null) {
                        name = methodAttr.Method;
                    }
                    methods[name] = method;
                }
            }
        }

        private string GetMessage(MethodInfo method) {
            var attr = (HubMethodAttribute)method.GetCustomAttributes(typeof(HubMethodAttribute), true).FirstOrDefault();
            if (attr == null) {
                return method.Name;
            }
            return attr.Method;
        }

        public class HubInvocationInfo {
            public string Hub { get; set; }
            public string Method { get; set; }
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
        private static string GetUrl(string url) {
            if (!url.EndsWith("/")) {
                url += "/";
            }
            return url + "signalr";
        }
    }
}
