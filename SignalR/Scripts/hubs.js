/*!
 * SignalR JavaScript Library v0.5.2
 * http://signalr.net/
 *
 * Copyright David Fowler and Damian Edwards 2012
 * Licensed under the MIT.
 * https://github.com/SignalR/SignalR/blob/master/LICENSE.md
 *
 */

/// <reference path="jquery-1.6.2.js" />
(function ($, window) {
    /// <param name="$" type="jQuery" />
    "use strict";

    if (typeof ($.signalR) !== "function") {
        throw "SignalR: SignalR is not loaded. Please ensure jquery.signalR-x.js is referenced before ~/signalr/hubs.";
    }

    var signalR = $.signalR;

    function makeProxyCallback(hub, callback) {
        return function () {
            // Update the hub state
            $.extend(hub, this.state);

            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
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
            callback = function (result) {
                // Update hub state from proxy state
                $.extend(hub, hub._.proxy.state);
                if ($.isFunction(userCallback)) {
                    userCallback.call(hub, result);
                }
            };

        if ($.isFunction(userCallback)) {
            // Replace user's callback with our own
            args = $.merge(args.splice(0, args.length - 1), [callback]);
        }

        // Update proxy state from hub state
        $.extend(hub._.proxy.state, copy(hub, ["_"]));

        return hub._.proxy.invoke.apply(hub._.proxy, $.merge([methodName], args));
    }

    // Create hub signalR instance
    $.extend(signalR, {
        /*
        myDemoHub: {
            _: {
                hubName: "MyDemoHub",
                ignoreMembers: ['serverMethod1', 'serverMethod2', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },
            serverMethod1: function(p1, p2) {
                return invoke(this, "ServerMethod1", $.makeArray(arguments));
            },
            serverMethod2: function(p1) {
                return invoke(this, "ServerMethod2", $.makeArray(arguments));;
            }
        }
        */
        /*hubs*/
    });

    signalR.hub = $.hubConnection("{serviceUrl}")
        .starting(function () {
            createHubProxies(signalR, this);
        });

}(window.jQuery, window));