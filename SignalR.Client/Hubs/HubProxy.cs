using System;
using System.Collections.Generic;
#if !WINDOWS_PHONE
using System.Dynamic;
#endif
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignalR.Client.Hubs {
#if !WINDOWS_PHONE
    public class HubProxy : DynamicObject, IHubProxy {
#else
    public class HubProxy : IHubProxy {
#endif
        private readonly string _hub;
        private readonly Connection _client;
        private readonly Dictionary<string, object> _state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public HubProxy(Connection client, string hub) {
            _client = client;
            _hub = hub;
        }

        public object this[string name] {
            get {
                object value;
                _state.TryGetValue(name, out value);
                return value;
            }
            set {
                _state[name] = value;
            }
        }

        public Task Invoke(string action, params object[] args) {
            return Invoke<object>(action, args);
        }

        public Task<T> Invoke<T>(string action, params object[] args) {
            var hubData = new HubData {
                Hub = _hub,
                Action = action,
                Data = args,
                State = _state
            };

            var value = JsonConvert.SerializeObject(hubData);

            return _client.Send<HubResult<T>>(value).ContinueWith(task => {
                if (task.Result != null) {

                    if (task.Result.Error != null) {
                        throw new InvalidOperationException(task.Result.Error);
                    }

                    HubResult<T> hubResult = task.Result;
                    foreach (var pair in hubResult.State) {
                        this[pair.Key] = pair.Value;
                    }

                    return hubResult.Result;
                }
                return default(T);
            });
        }

#if !WINDOWS_PHONE
        public override bool TrySetMember(SetMemberBinder binder, object value) {
            _state[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            _state.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            result = Invoke(binder.Name, args);
            return true;
        }
#endif

        public class HubData {
            public Dictionary<string, object> State { get; set; }
            public object[] Data { get; set; }
            public string Action { get; set; }
            public string Hub { get; set; }
        }

        public class HubResult<T> {
            public T Result { get; set; }
            public string Error { get; set; }
            public IDictionary<string, object> State { get; set; }
        }
    }
}
