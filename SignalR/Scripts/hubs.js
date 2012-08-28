/*!
 * SignalR JavaScript Library v0.5.3.3.3.3.3.3.3
 * http://signalr.net/
 *
 * Copyright David Fowler and Damian Edwards 2012
 * Licensed under the MIT.
 * https://github.com/SignalR/SignalR/blob/master/LICENSE.md
 *
 */

/// <reference path="..\..\SignalR.Client.JS\Scripts\jquery-1.6.2.js" />
/// <reference path="jquery.signalR.js" />
(function ($, window) {
    /// <param name="$" type="jQuery" />
    "use strict";

    if (typeof ($.signalR) !== "function") {
        throw "SignalR: SignalR is not loaded. Please ensure jquery.signalR-x.js is referenced before ~/signalr/hubs.";
    }

    var signalR = $.signalR;

    function makeProxyCallback(hub, callback) {
        return function () {
            //updateHubState(hub, this.state);

            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
    }

    function updateHubState(hub, newState) {
        var oldState = hub.oldState;

        if (!oldState) {
            // First time updating client state, just copy it all over
            hub.oldState = $.extend({}, newState);
            return;
        }

        // Compare the old client state to current client state and preserve any changes
        for (var key in newState) {
            if (typeof (oldState[key]) !== "undefined" && oldState[key] === newState[key]) {
                // Key is different between old state and new state thus it's changed locally
                continue;
            }
            hub.state[key] = newState[key];
            hub.oldState[key] = newState[key];
        }
    }

    function createHubProxies(instance, hubConnection) {
        var key, hub, memberKey, memberValue, proxy;

        for (key in instance) {
            if (instance.hasOwnProperty(key)) {
                hub = instance[key];

                if (!(hub.hubName)) {
                    // Not a client hub
                    continue;
                }

                // Create and store the hub proxy
                //hub = hubConnection.createProxy(hub.hubName);

                // Loop through all members on the hub and find client hub functions to subscribe to
                for (memberKey in hub.client) {
                    if (hub.client.hasOwnProperty(memberKey)) {
                        memberValue = hub.client[memberKey];

                        if ($.type(memberValue) !== "function") {
                            // Not a client hub function
                            continue;
                        }

                        // Subscribe to the hub event for this method
                        hub.on(memberKey, makeProxyCallback(hub, memberValue));
                    }
                }
            }
        }
    }

    function invoke(hub, methodName, args) {
        if (!hub) {
            if (hub.connection.state === signalR.connectionState.disconnected) {
                // Connection hasn't been started yet
                throw "SignalR: Connection must be started before data can be sent. Call .start() before .send()";
            }

            if (hub.connection.state === signalR.connectionState.connecting) {
                // Connection hasn't been started yet
                throw "SignalR: Connection has not been fully initialized. Use .start().done() or .start().fail() to run logic after the connection has started.";
            }
        }

        // Extract user callback from args
        var userCallback = args[args.length - 1], // last argument
            callback;

        if ($.isFunction(userCallback)) {
            // Replace user's callback with our own
            callback = function (result) {
                if ($.isFunction(userCallback)) {
                    userCallback.call(hub, result); // TODO: What is this supposed to call?
                }
            };
            args = $.merge(args.splice(0, args.length - 1), [callback]);
        }

        return hub.invoke.apply(hub, $.merge([methodName], args));
    }

    signalR.hub = $.hubConnection("{serviceUrl}")
        .starting(function () {
            createHubProxies(signalR, this);
        });

    /*hubs*/



}(window.jQuery, window));