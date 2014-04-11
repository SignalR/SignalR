﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubProxy : IHubProxy
    {
        private readonly string _hubName;
        private readonly IHubConnection _connection;
        private readonly Dictionary<string, object> _state = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);

        public HubProxy(IHubConnection connection, string hubName)
        {
            _connection = connection;
            _hubName = hubName;
        }

        public object this[string name]
        {
            get
            {
                lock (_state)
                {
                    object value;
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

        public JsonSerializer JsonSerializer
        {
            get { return _connection.JsonSerializer; }
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
            return Invoke<T, object>(method, onProgress: null, args: args);
        }

        public Task Invoke<T>(string method, Action<T> onProgress, params object[] args)
        {
            return Invoke<object, T>(method, onProgress, args);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flown to the caller")]
        public Task<TResult> Invoke<TResult, TProgress>(string method, Action<TProgress> onProgress, params object[] args)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            var tcs = new TaskCompletionSource<TResult>();
            var callbackId = _connection.RegisterCallback(result =>
            {
                if (result != null)
                {
                    if (result.Error != null)
                    {
                        if (result.IsHubException.HasValue && result.IsHubException.Value)
                        {
                            // A HubException was thrown
                            tcs.TrySetException(new HubException(result.Error, result.ErrorData));
                        }
                        else
                        {
                            tcs.TrySetException(new InvalidOperationException(result.Error));
                        }
                    }
                    else
                    {
                        try
                        {
                            if (result.State != null)
                            {
                                foreach (var pair in result.State)
                                {
                                    this[pair.Key] = pair.Value;
                                }
                            }

                            if (result.Progress != null)
                            {
                                onProgress(HubProxyExtensions.Convert<TProgress>(result.Progress.Data, JsonSerializer));
                            }
                            else if (result.Result != null)
                            {
                                tcs.TrySetResult(HubProxyExtensions.Convert<TResult>(result.Result, JsonSerializer));
                            }
                            else
                            {
                                tcs.TrySetResult(default(TResult));
                            }
                        }
                        catch (Exception ex)
                        {
                            // If we failed to set the result for some reason or to update
                            // state then just fail the tcs.
                            tcs.TrySetUnwrappedException(ex);
                        }
                    }
                }
                else
                {
                    tcs.TrySetCanceled();
                }
            });

            var hubData = new HubRequest
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                Id = callbackId
            };

            if (_state.Count != 0)
            {
                hubData.State = _state;
            }

            var value = _connection.JsonSerializeObject(hubData);

            _connection.Send(value).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    _connection.RemoveCallback(callbackId);
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    _connection.RemoveCallback(callbackId);
                    tcs.TrySetUnwrappedException(task.Exception);
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);

            return tcs.Task;
        }

        public void InvokeEvent(string eventName, IList<object> args)
        {
            Subscription subscription;
            if (_subscriptions.TryGetValue(eventName, out subscription))
            {
                subscription.OnReceived(args);
            }
        }
    }
}
