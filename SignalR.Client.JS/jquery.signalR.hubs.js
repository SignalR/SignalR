/*global window:false */
/// <reference path="jquery.signalR.core.js" />

(function ($, window) {
    "use strict";

    // we use a global id for tracking callbacks so the server doesn't have to send extra info like hub name
    var callbackId = 0,
        callbacks = {},
        eventNamespace = "proxy.";

    // Array.prototype.map
    if (!Array.prototype.hasOwnProperty("map")) {
        Array.prototype.map = function (fun, thisp) {
            var arr = this,
                i,
                length = arr.length,
                result = [];
            for (i = 0; i < length; i += 1) {
                if (arr.hasOwnProperty(i)) {
                    result[i] = fun.call(thisp, arr[i], i, arr);
                }
            }
            return result;
        };
    }

    function getArgValue(a) {
        return $.isFunction(a) ? null : ($.type(a) === "undefined" ? null : a);
    }

    // hubProxy
    function hubProxy(hubConnection, hubName) {
        /// <summary>
        ///     Creates a new proxy object for the given hub connection that can be used to invoke
        ///     methods on server hubs and handle client method invocation requests from the server.
        /// </summary>
        return new hubProxy.fn.init(hubConnection, hubName);
    }

    hubProxy.fn = hubProxy.prototype = {
        init: function (connection, hubName) {
            this.state = {};
            this.connection = connection;
            this.hubName = hubName;
            this.subscribed = false;
        },

        on: function (eventName, callback) {
            /// <summary>Wires up a callback to be invoked when a invocation request is received from the server hub.</summary>
            /// <param name="eventName" type="String">The name of the hub event to register the callback for.</param>
            /// <param name="callback" type="Function">The callback to be invoked.</param>
            var self = this;

            // Normalize the event name to lowercase
            eventName = eventName.toLowerCase();

            $(self).bind(eventNamespace + eventName, function (e, data) {
                callback.apply(self, data);
            });
            self.subscribed = true;
            return self;
        },

        invoke: function (methodName) {
            /// <summary>Invokes a server hub method with the given arguments.</summary>
            /// <param name="methodName" type="String">The name of the server hub method.</param>

            var self = this,
                args = $.makeArray(arguments).slice(1),
                userCallback = args[args.length - 1], // last argument
                methodArgs = $.type(userCallback) === "function" ? args.slice(0, args.length - 1) /* all but last */ : args,
                argValues = methodArgs.map(getArgValue),
                data = { hub: self.hubName, method: methodName, args: argValues, state: self.state, id: callbackId },
                d = $.Deferred(),
                callback = function (result) {
                    // Update the hub state
                    $.extend(this.state, result.State);

                    if (result.Error) {
                        // Server hub method threw an exception, log it & reject the deferred
                        if (result.StackTrace) {
                            self.connection.log(result.Error + "\n" + result.StackTrace);
                        }
                        d.rejectWith(self, [result.Error]);
                    } else {
                        // Server invocation succeeded, invoke any user callback & resolve the deferred
                        if ($.type(userCallback) === "function") {
                            userCallback.call(self, result.Result);
                        }
                        d.resolveWith(self, [result.Result]);
                    }
                };

            callbacks[callbackId.toString()] = { scope: self, method: callback };
            callbackId += 1;
            self.connection.send(window.JSON.stringify(data));

            return d.promise();
        }
    };

    hubProxy.fn.init.prototype = hubProxy.fn;


    // hubConnection
    function hubConnection(url) {
        /// <summary>Creates a new hub connection.</summary>
        /// <param name="url" type="String">[Optional] The hub route url, defaults to "/signalr"</param>
        if (!url) {
            url = "/signalr";
        }
        return new hubConnection.fn.init(url);
    }

    hubConnection.fn = hubConnection.prototype = $.connection();

    hubConnection.fn.init = function (url, qs, logging) {
        var connection = this;

        // Call the base constructor
        $.signalR.fn.init.call(connection, url, qs, logging);

        // Object to store hub proxies for this connection
        connection.proxies = {};

        // Wire up the sending handler
        connection.sending(function () {
            // Set the connection's data object with all the hub proxies with active subscriptions.
            // These proxies will receive notifications from the server.
            var subscribedHubs = [];

            $.each(this.proxies, function (key) {
                if (this.subscribed) {
                    subscribedHubs.push({ name: key });
                }
            });

            this.data = window.JSON.stringify(subscribedHubs);
        });

        // Wire up the received handler
        connection.received(function (data) {
            var proxy, dataCallbackId, callback, hubName, eventName;
            if (!data) {
                return;
            }

            if (typeof (data.Id) !== "undefined") {
                // We received the return value from a server method invocation, look up callback by id and call it
                dataCallbackId = data.Id.toString();
                callback = callbacks[dataCallbackId];
                if (callback) {
                    // Delete the callback from the proxy
                    callbacks[dataCallbackId] = null;
                    delete callbacks[dataCallbackId];

                    // Invoke the callback
                    callback.method.call(callback.scope, data);
                }
            } else {
                // Normalize the names to lowercase
                hubName = data.Hub.toLowerCase();
                eventName = data.Method.toLowerCase();

                // We received a client invocation request, i.e. broadcast from server hub
                // Trigger the local invocation event
                proxy = this.proxies[hubName];
                // Update the hub state
                $.extend(proxy.state, data.State);
                $(proxy).trigger(eventNamespace + eventName, [data.Args]);
            }
        });
    };

    hubConnection.fn.createProxy = function (hubName) {
        /// <summary>
        ///     Creates a new proxy object for the given hub connection that can be used to invoke
        ///     methods on server hubs and handle client method invocation requests from the server.
        /// </summary>
        /// <paramater name="hubName" type="String">
        ///     The name of the hub on the server to create the proxy for.
        /// </parameter>

        // Normalize the name to lowercase
        hubName = hubName.toLowerCase();

        var proxy = this.proxies[hubName];
        if (!proxy) {
            proxy = hubProxy(this, hubName);
            this.proxies[hubName] = proxy;
        }
        return proxy;
    };

    hubConnection.fn.init.prototype = hubConnection.fn;

    $.hubConnection = hubConnection;

} (window.jQuery, window));
