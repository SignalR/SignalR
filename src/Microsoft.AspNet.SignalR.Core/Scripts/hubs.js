/*!
 * SignalR JavaScript Library v1.0.0
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
        throw new Error("SignalR: SignalR is not loaded. Please ensure jquery.signalR-x.js is referenced before ~/signalr/hubs.");
    }

    var signalR = $.signalR;

    function makeProxyCallback(hub, callback) {
        return function () {
            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
    }

    function createHubProxies(instance) {
        var key, hub, memberKey, memberValue;

        for (key in instance) {
            if (instance.hasOwnProperty(key)) {
                hub = instance[key];

                if (!(hub.hubName)) {
                    // Not a client hub
                    continue;
                }

                // Loop through all members on the hub and find client hub functions to subscribe to
                for (memberKey in hub.client) {
                    if (hub.client.hasOwnProperty(memberKey)) {
                        memberValue = hub.client[memberKey];
                        
                        if (!$.isFunction(memberValue)) {
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

    signalR.hub = $.hubConnection("{serviceUrl}", { useDefaultPath: false })
        .starting(function () {
            // Subscribe and create the hub proxies
            createHubProxies(signalR);

            // Set the connection's data object with all the hub proxies with active subscriptions.
            // These proxies will receive notifications from the server.
            var subscribedHubs = [];

            $.each(this.proxies, function (key) {
                if (this.hasSubscriptions()) {
                    subscribedHubs.push({ name: key });
                }
            });

            this.data = window.JSON.stringify(subscribedHubs);
        });

    /*hubs*/

}(window.jQuery, window));