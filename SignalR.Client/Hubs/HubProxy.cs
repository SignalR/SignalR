using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignalR.Client.Hubs
{
    public class HubProxy : DynamicObject, IHubProxy
    {
        private readonly string _hubName;
        private readonly HubConnection _connection;
        private readonly Dictionary<string, object> _state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Action<object[]>> _subscriptions = new Dictionary<string, Action<object[]>>(StringComparer.OrdinalIgnoreCase);

        public HubProxy(HubConnection connection, string hubName)
        {
            _connection = connection;
            _hubName = hubName;
        }

        public object this[string name]
        {
            get
            {
                object value;
                _state.TryGetValue(name, out value);
                return value;
            }
            set
            {
                _state[name] = value;
            }
        }

        public void Subscribe(string eventName, Action<object[]> subscription)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            if (subscription == null)
            {
                throw new ArgumentNullException("subscription");
            }

            _subscriptions.Add(eventName, subscription);
        }

        public void Unsubscribe(string eventName)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            _subscriptions.Remove(eventName);
        }

        public Task Invoke(string action, params object[] args)
        {
            return Invoke<object>(action, args);
        }

        public Task<T> Invoke<T>(string action, params object[] args)
        {
            var hubData = new HubData
            {
                Hub = _hubName,
                Action = action,
                Data = args,
                State = _state
            };

            var value = JsonConvert.SerializeObject(hubData);

            return _connection.Send<HubResult<T>>(value).Success(task =>
            {
                if (task.Result != null)
                {
                    if (task.Result.Error != null)
                    {
                        throw new InvalidOperationException(task.Result.Error);
                    }

                    HubResult<T> hubResult = task.Result;
                    foreach (var pair in hubResult.State)
                    {
                        this[pair.Key] = pair.Value;
                    }

                    return hubResult.Result;
                }
                return default(T);
            });
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _state[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            _state.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoke(binder.Name, args);
            return true;
        }

        public void InvokeMethod(string eventName, object[] args)
        {
            Action<object[]> subscription;
            if (_subscriptions.TryGetValue(eventName, out subscription))
            {
                subscription(args);
            }
        }

        public IEnumerable<string> GetSubscriptions()
        {
            return _subscriptions.Keys;
        }
        
        private class HubData
        {
            public Dictionary<string, object> State { get; set; }
            public object[] Data { get; set; }
            public string Action { get; set; }
            public string Hub { get; set; }
        }

        private class HubResult<T>
        {
            public T Result { get; set; }
            public string Error { get; set; }
            public IDictionary<string, object> State { get; set; }
        }
    }
}
