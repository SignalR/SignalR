/*!
* SignalR JavaScript Library v0.5
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
        throw "SignalR: SignalR is not loaded. Please ensure jquery.signalR.js is referenced before ~/signalr/hubs.";
    }

    var hubs = {},
        signalR = $.signalR,
        callbackId = 0,
        callbacks = {};

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

    function executeCallback(hubName, fn, args, state) {
        var hub = hubs[hubName],
            hubMethod;

        if (hub) {
            signalR.hub.processState(hubName, hub.obj, state);

            hubMethod = hub.obj[fn];
            if (hubMethod) {
                hubMethod.apply(hub.obj, args);
            }
        }
    }

    function updateClientMembers(instance) {
        var newHubs = {},
            obj,
            memberValue,
            key,
            memberKey,
            hasSubscription = false;

        for (key in instance) {
            if (instance.hasOwnProperty(key)) {
                // This is a client hub
                obj = instance[key];

                if ($.type(obj) !== "object" ||
                        $.inArray(key, ["prototype", "constructor", "fn", "hub", "transports"]) >= 0) {
                    continue;
                }

                hasSubscription = false;

                for (memberKey in obj) {
                    if (obj.hasOwnProperty(memberKey)) {
                        memberValue = obj[memberKey];

                        if (memberKey === "_" ||
                                $.type(memberValue) !== "function" ||
                                $.inArray(memberKey, obj._.ignoreMembers) >= 0) {
                            continue;
                        }

                        hasSubscription = true;
                        break;
                    }
                }

                if (hasSubscription === true) {
                    newHubs[obj._.hubName] = { obj: obj };
                }
            }
        }

        hubs = {};
        $.extend(hubs, newHubs);
    }

    function getArgValue(a) {
        return $.isFunction(a)
            ? null
            : ($.type(a) === "undefined"
                ? null
                : a);
    }

    function copy(obj, exclude) {
        var newObj = {};
        $.each(obj, function (key, value) {
            if ($.inArray(key, exclude) === -1) {
                // We don't use "this" because browser suck!
                newObj[key] = value;
            }
        });

        return newObj;
    }

    function serverCall(hub, methodName, args) {
        var callback = args[args.length - 1], // last argument
            methodArgs = $.type(callback) === "function"
                ? args.slice(0, -1) // all but last
                : args,
            argValues = methodArgs.map(getArgValue),
            data = { hub: hub._.hubName, method: methodName, args: argValues, state: copy(hub, ["_"]), id: callbackId },
            d = $.Deferred(),
            cb = function (result) {
                signalR.hub.processState(hub._.hubName, hub, result.State);

                if (result.Error) {
                    if (result.StackTrace) {
                        signalR.hub.log(result.Error + "\n" + result.StackTrace);
                    }
                    d.rejectWith(hub, [result.Error]);
                } else {
                    if ($.type(callback) === "function") {
                        callback.call(hub, result.Result);
                    }
                    d.resolveWith(hub, [result.Result]);
                }
            };

        callbacks[callbackId.toString()] = { scope: hub, callback: cb };
        callbackId += 1;
        hub._.connection().send(window.JSON.stringify(data));
        return d;
    }

    // Create hub signalR instance
    $.extend(signalR, {
        /*hubs*/
    });

    signalR.hub = signalR("{serviceUrl}")
        .starting(function () {
            updateClientMembers(signalR);
        })
        .sending(function () {
            var localHubs = [];

            $.each(hubs, function (key) {
                localHubs.push({ name: key });
            });

            this.data = window.JSON.stringify(localHubs);
        })
        .received(function (result) {
            var callbackId, cb;
            if (result) {
                if (!result.Id) {
                    executeCallback(result.Hub, result.Method, result.Args, result.State);
                } else {
                    callbackId = result.Id.toString();
                    cb = callbacks[callbackId];
                    if (cb) {
                        callbacks[callbackId] = null;
                        delete callbacks[callbackId];
                        cb.callback.call(cb.scope, result);
                    }
                }
            }
        });

    signalR.hub.processState = function (hubName, left, right) {
        $.extend(left, right);
    };

} (window.jQuery, window));