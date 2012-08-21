/*!
 * SignalR JavaScript Library v0.5.3
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
            updateHubState(hub, this.state);
            
            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
    }

    function updateHubState(hub, newState) {
        var oldState = hub._.oldState;

        if (!oldState) {
            // First time updating client state, just copy it all over
            $.extend(hub, newState);
            hub._.oldState = $.extend({}, newState);
            return;
        } 

        // Compare the old client state to current client state and preserve any changes
        for (var key in newState) {
            if (typeof (oldState[key]) !== "undefined" && oldState[key] === newState[key]) {
                // Key is different between old state and new state thus it's changed locally
                continue;
            }
            hub[key] = newState[key];
            hub._.oldState[key] = newState[key];
        }
    }

    function createHubProxies(instance, hubConnection) {
        var key, hub, memberKey, memberValue, proxy;

        for (key in instance) {
            if (instance.hasOwnProperty(key)) {
                hub = instance[key];

                if (!(hub._ && hub._.hubName)) {
                    // Not a client hub
                    continue;
                }

                // Create and store the hub proxy
                hub._.proxy = hubConnection.createProxy(hub._.hubName);

                // Loop through all members on the hub and find client hub functions to subscribe to
                for (memberKey in hub) {
                    if (hub.hasOwnProperty(memberKey)) {
                        memberValue = hub[memberKey];

                        if (memberKey === "_" || $.type(memberValue) !== "function"
                            || $.inArray(memberKey, hub._.ignoreMembers) >= 0) {
                            // Not a client hub function
                            continue;
                        }
                        
                        // Subscribe to the hub event for this method
                        hub._.proxy.on(memberKey, makeProxyCallback(hub, memberValue));
                    }
                }
            }
        }
    }

    function copy(obj, exclude) {
        var newObj = {};
        $.each(obj, function (key, value) {
            if (!$.isFunction(value) && $.inArray(key, exclude) === -1) {
                // We don't use "this" because browsers suck!
                newObj[key] = value;
            }
        });
        return newObj;
    }

    function invoke(hub, methodName, args) {
        // Extract user callback from args
        var userCallback = args[args.length - 1], // last argument
            callback,
            connection = hub._.connection();

        if ($.isFunction(userCallback)) {
            // Replace user's callback with our own
            callback = function (result) {
                // Update hub state from proxy state
                $.extend(hub, hub._.proxy.state);
                if ($.isFunction(userCallback)) {
                    userCallback.call(hub, result);
                }
            };
            args = $.merge(args.splice(0, args.length - 1), [callback]);
        }

        if (!hub._.proxy) {
            if (connection.state === signalR.connectionState.disconnected) {
                // Connection hasn't been started yet
                throw "SignalR: Connection must be started before data can be sent. Call .start() before .send()";
            }

            if (connection.state === signalR.connectionState.connecting) {
                // Connection hasn't been started yet
                throw "SignalR: Connection has not been fully initialized. Use .start().done() or .start().fail() to run logic after the connection has started.";
            }
        }

        // Update proxy state from hub state
        $.extend(hub._.proxy.state, copy(hub, ["_"]));

        return hub._.proxy.invoke.apply(hub._.proxy, $.merge([methodName], args));
    }

    /*hubs*/

    signalR.hub = $.hubConnection("{serviceUrl}")
        .starting(function () {
            createHubProxies(signalR, this);
        });

}(window.jQuery, window));