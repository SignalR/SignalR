// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
#if !WINDOWS_PHONE && !NET35
using System.Dynamic;
#endif
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubProxy :
#if !WINDOWS_PHONE && !NET35
 DynamicObject,
#endif
 IHubProxy
    {
        private readonly string _hubName;
        private readonly IConnection _connection;
        private readonly Dictionary<string, JToken> _state = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);

        public HubProxy(IConnection connection, string hubName)
        {
            _connection = connection;
            _hubName = hubName;
        }

        public JToken this[string name]
        {
            get
            {
                lock (_state)
                {
                    JToken value;
                    _state.TryGetValue(name, out value);
                    return value;
                }
            }
            set
            {
                lock (_state)
                {
                    _state[name] = value;
                }
            }
        }

        public Subscription Subscribe(string eventName)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            Subscription subscription;
            if (!_subscriptions.TryGetValue(eventName, out subscription))
            {
                subscription = new Subscription();
                _subscriptions.Add(eventName, subscription);
            }

            return subscription;
        }

        public Task Invoke(string method, params object[] args)
        {
            return Invoke<object>(method, args);
        }

        public Task<T> Invoke<T>(string method, params object[] args)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            var tokenifiedArguments = new JToken[args.Length];
            for (int i = 0; i < tokenifiedArguments.Length; i++)
            {
                tokenifiedArguments[i] = JToken.FromObject(args[i]);
            }

            var hubData = new ServerHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = tokenifiedArguments,
                State = _state
            };

            var value = JsonConvert.SerializeObject(hubData);

            return _connection.Send<HubResult<T>>(value).Then(result =>
            {
                if (result != null)
                {
                    if (result.Error != null)
                    {
                        throw new InvalidOperationException(result.Error);
                    }

                    if (result.State != null)
                    {
                        foreach (var pair in result.State)
                        {
                            this[pair.Key] = pair.Value;
                        }
                    }

                    return result.Result;
                }
                return default(T);
            });
        }

#if !WINDOWS_PHONE && !NET35
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value as JToken ?? JToken.FromObject(value);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoke(binder.Name, args);
            return true;
        }
#endif

        public void InvokeEvent(string eventName, JToken[] args)
        {
            Subscription eventObj;
            if (_subscriptions.TryGetValue(eventName, out eventObj))
            {
                eventObj.OnData(args);
            }
        }
    }
}
