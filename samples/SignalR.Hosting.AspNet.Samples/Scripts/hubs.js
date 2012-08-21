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

    signalR.chat = {
        _: {
            hubName: 'Chat',
            ignoreMembers: ['getUsers', 'join', 'send'],
            connection: function () { return signalR.hub; }
        },

        join: function () {
            /// <summary>Calls the Join method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "Join", $.makeArray(arguments));
        },

        send: function (content) {
            /// <summary>Calls the Send method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="content" type="String">Server side type is System.String</param>
            return invoke(this, "Send", $.makeArray(arguments));
        },

        getUsers: function () {
            /// <summary>Calls the GetUsers method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "GetUsers", $.makeArray(arguments));
        }
    };

    signalR.demo = {
        _: {
            hubName: 'demo',
            ignoreMembers: ['addToGroups', 'complexArray', 'complexType', 'doSomethingAndCallError', 'dynamicInvoke', 'dynamicTask', 'genericTaskTypedAsPlain', 'genericTaskWithException', 'getValue', 'mispelledClientMethod', 'multipleCalls', 'overload', 'passingDynamicComplex', 'plainTask', 'readStateValue', 'setStateValue', 'simpleArray', 'taskWithException', 'testGuid', 'unsupportedOverload'],
            connection: function () { return signalR.hub; }
        },

        getValue: function () {
            /// <summary>Calls the GetValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "GetValue", $.makeArray(arguments));
        },

        addToGroups: function () {
            /// <summary>Calls the AddToGroups method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "AddToGroups", $.makeArray(arguments));
        },

        doSomethingAndCallError: function () {
            /// <summary>Calls the DoSomethingAndCallError method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "DoSomethingAndCallError", $.makeArray(arguments));
        },

        dynamicTask: function () {
            /// <summary>Calls the DynamicTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "DynamicTask", $.makeArray(arguments));
        },

        plainTask: function () {
            /// <summary>Calls the PlainTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "PlainTask", $.makeArray(arguments));
        },

        genericTaskTypedAsPlain: function () {
            /// <summary>Calls the GenericTaskTypedAsPlain method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "GenericTaskTypedAsPlain", $.makeArray(arguments));
        },

        taskWithException: function () {
            /// <summary>Calls the TaskWithException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "TaskWithException", $.makeArray(arguments));
        },

        genericTaskWithException: function () {
            /// <summary>Calls the GenericTaskWithException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "GenericTaskWithException", $.makeArray(arguments));
        },

        simpleArray: function (nums) {
            /// <summary>Calls the SimpleArray method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="nums" type="Object">Server side type is System.Int32[]</param>
            return invoke(this, "SimpleArray", $.makeArray(arguments));
        },

        readStateValue: function () {
            /// <summary>Calls the ReadStateValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "ReadStateValue", $.makeArray(arguments));
        },

        setStateValue: function (value) {
            /// <summary>Calls the SetStateValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="value" type="String">Server side type is System.String</param>
            return invoke(this, "SetStateValue", $.makeArray(arguments));
        },

        complexArray: function (people) {
            /// <summary>Calls the ComplexArray method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="people" type="Object">Server side type is SignalR.Samples.Hubs.DemoHub.DemoHub+Person[]</param>
            return invoke(this, "ComplexArray", $.makeArray(arguments));
        },

        complexType: function (p) {
            /// <summary>Calls the ComplexType method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="p" type="Object">Server side type is SignalR.Samples.Hubs.DemoHub.DemoHub+Person</param>
            return invoke(this, "ComplexType", $.makeArray(arguments));
        },

        passingDynamicComplex: function (p) {
            /// <summary>Calls the PassingDynamicComplex method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="p" type="Object">Server side type is System.Object</param>
            return invoke(this, "PassingDynamicComplex", $.makeArray(arguments));
        },

        multipleCalls: function () {
            /// <summary>Calls the MultipleCalls method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "MultipleCalls", $.makeArray(arguments));
        },

        overload: function () {
            /// <summary>Calls the Overload method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "Overload", $.makeArray(arguments));
        },

        unsupportedOverload: function (x) {
            /// <summary>Calls the UnsupportedOverload method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="x" type="String">Server side type is System.String</param>
            return invoke(this, "UnsupportedOverload", $.makeArray(arguments));
        },

        testGuid: function () {
            /// <summary>Calls the TestGuid method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "TestGuid", $.makeArray(arguments));
        },

        dynamicInvoke: function (method) {
            /// <summary>Calls the DynamicInvoke method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="method" type="String">Server side type is System.String</param>
            return invoke(this, "DynamicInvoke", $.makeArray(arguments));
        },

        mispelledClientMethod: function () {
            /// <summary>Calls the MispelledClientMethod method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "MispelledClientMethod", $.makeArray(arguments));
        }
    };

    signalR.drawingPad = {
        _: {
            hubName: 'DrawingPad',
            ignoreMembers: ['drawLine', 'join'],
            connection: function () { return signalR.hub; }
        },

        join: function () {
            /// <summary>Calls the Join method on the server-side DrawingPad hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "Join", $.makeArray(arguments));
        },

        drawLine: function (data) {
            /// <summary>Calls the DrawLine method on the server-side DrawingPad hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="data" type="Object">Server side type is SignalR.Hosting.AspNet.Samples.Hubs.DrawingPad.DrawingPad+Line</param>
            return invoke(this, "DrawLine", $.makeArray(arguments));
        }
    };

    signalR.hubBench = {
        _: {
            hubName: 'HubBench',
            ignoreMembers: ['hitMe', 'hitUs'],
            connection: function () { return signalR.hub; }
        },

        hitMe: function (start, clientCalls, connectionId) {
            /// <summary>Calls the HitMe method on the server-side HubBench hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="start" type="Number">Server side type is System.Int64</param>
            /// <param name="clientCalls" type="Number">Server side type is System.Int32</param>
            /// <param name="connectionId" type="String">Server side type is System.String</param>
            return invoke(this, "HitMe", $.makeArray(arguments));
        },

        hitUs: function (start, clientCalls) {
            /// <summary>Calls the HitUs method on the server-side HubBench hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="start" type="Number">Server side type is System.Int64</param>
            /// <param name="clientCalls" type="Number">Server side type is System.Int32</param>
            return invoke(this, "HitUs", $.makeArray(arguments));
        }
    };

    signalR.mouseTracking = {
        _: {
            hubName: 'MouseTracking',
            ignoreMembers: ['join', 'move'],
            connection: function () { return signalR.hub; }
        },

        join: function () {
            /// <summary>Calls the Join method on the server-side MouseTracking hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "Join", $.makeArray(arguments));
        },

        move: function (x, y) {
            /// <summary>Calls the Move method on the server-side MouseTracking hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="x" type="Number">Server side type is System.Int32</param>
            /// <param name="y" type="Number">Server side type is System.Int32</param>
            return invoke(this, "Move", $.makeArray(arguments));
        }
    };

    signalR.shapeShare = {
        _: {
            hubName: 'ShapeShare',
            ignoreMembers: ['changeShape', 'changeUserName', 'createShape', 'deleteAllShapes', 'deleteShape', 'getShapes', 'join'],
            connection: function () { return signalR.hub; }
        },

        getShapes: function () {
            /// <summary>Calls the GetShapes method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "GetShapes", $.makeArray(arguments));
        },

        join: function (userName) {
            /// <summary>Calls the Join method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="userName" type="String">Server side type is System.String</param>
            return invoke(this, "Join", $.makeArray(arguments));
        },

        changeUserName: function (currentUserName, newUserName) {
            /// <summary>Calls the ChangeUserName method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="currentUserName" type="String">Server side type is System.String</param>
            /// <param name="newUserName" type="String">Server side type is System.String</param>
            return invoke(this, "ChangeUserName", $.makeArray(arguments));
        },

        createShape: function (type) {
            /// <summary>Calls the CreateShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="type" type="String">Server side type is System.String</param>
            return invoke(this, "CreateShape", $.makeArray(arguments));
        },

        changeShape: function (id, x, y, w, h) {
            /// <summary>Calls the ChangeShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="id" type="String">Server side type is System.String</param>
            /// <param name="x" type="Number">Server side type is System.Int32</param>
            /// <param name="y" type="Number">Server side type is System.Int32</param>
            /// <param name="w" type="Number">Server side type is System.Int32</param>
            /// <param name="h" type="Number">Server side type is System.Int32</param>
            return invoke(this, "ChangeShape", $.makeArray(arguments));
        },

        deleteShape: function (id) {
            /// <summary>Calls the DeleteShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="id" type="String">Server side type is System.String</param>
            return invoke(this, "DeleteShape", $.makeArray(arguments));
        },

        deleteAllShapes: function () {
            /// <summary>Calls the DeleteAllShapes method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return invoke(this, "DeleteAllShapes", $.makeArray(arguments));
        }
    };

    signalR.status = {
        _: {
            hubName: 'Status',
            ignoreMembers: [],
            connection: function () { return signalR.hub; }
        }
    };

    signalR.hub = $.hubConnection("/signalr")
        .starting(function () {
            createHubProxies(signalR, this);
        });

}(window.jQuery, window));