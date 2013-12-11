/*!
 * ASP.NET SignalR JavaScript Library v2.0.0-beta1
 * http://signalr.net/
 *
 * Copyright Microsoft Open Technologies, Inc. All rights reserved.
 * Licensed under the Apache 2.0
 * https://github.com/SignalR/SignalR/blob/master/LICENSE.md
 *
 */

/// <reference path="..\..\SignalR.Client.JS\Scripts\jquery-1.6.4.js" />
/// <reference path="jquery.signalR.js" />
(function ($, window) {
    /// <param name="$" type="jQuery" />
    "use strict";

    if (typeof ($.signalR) !== "function") {
        throw new Error("SignalR: SignalR is not loaded. Please ensure jquery.signalR-x.js is referenced before ~/signalr/js.");
    }

    var signalR = $.signalR;

    function makeProxyCallback(hub, callback) {
        return function () {
            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
    }

    function registerHubProxies(instance, shouldSubscribe) {
        var key, hub, memberKey, memberValue, subscriptionMethod;

        for (key in instance) {
            if (instance.hasOwnProperty(key)) {
                hub = instance[key];

                if (!(hub.hubName)) {
                    // Not a client hub
                    continue;
                }

                if (shouldSubscribe) {
                    // We want to subscribe to the hub events
                    subscriptionMethod = hub.on;
                }
                else {
                    // We want to unsubscribe from the hub events
                    subscriptionMethod = hub.off;
                }

                // Loop through all members on the hub and find client hub functions to subscribe/unsubscribe
                for (memberKey in hub.client) {
                    if (hub.client.hasOwnProperty(memberKey)) {
                        memberValue = hub.client[memberKey];

                        if (!$.isFunction(memberValue)) {
                            // Not a client hub function
                            continue;
                        }

                        subscriptionMethod.call(hub, memberKey, makeProxyCallback(hub, memberValue));
                    }
                }
            }
        }
    }

    $.hubConnection.prototype.createHubProxies = function () {
        var proxies = {};
        this.starting(function () {
            // Register the hub proxies as subscribed
            // (instance, shouldSubscribe)
            registerHubProxies(proxies, true);

            this._registerSubscribedHubs();
        }).disconnected(function () {
            // Unsubscribe all hub proxies when we "disconnect".  This is to ensure that we do not re-add functional call backs.
            // (instance, shouldSubscribe)
            registerHubProxies(proxies, false);
        });

        proxies.adminAuthHub = this.createHubProxy('adminAuthHub'); 
        proxies.adminAuthHub.client = { };
        proxies.adminAuthHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side AdminAuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.adminAuthHub.invoke.apply(proxies.adminAuthHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        proxies.authHub = this.createHubProxy('authHub'); 
        proxies.authHub.client = { };
        proxies.authHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side AuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.authHub.invoke.apply(proxies.authHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        proxies.chat = this.createHubProxy('chat'); 
        proxies.chat.client = { };
        proxies.chat.server = {
            getUsers: function () {
            /// <summary>Calls the GetUsers method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.chat.invoke.apply(proxies.chat, $.merge(["GetUsers"], $.makeArray(arguments)));
             },

            join: function () {
            /// <summary>Calls the Join method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.chat.invoke.apply(proxies.chat, $.merge(["Join"], $.makeArray(arguments)));
             },

            send: function (content) {
            /// <summary>Calls the Send method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"content\" type=\"String\">Server side type is System.String</param>
                return proxies.chat.invoke.apply(proxies.chat, $.merge(["Send"], $.makeArray(arguments)));
             }
        };

        proxies.countingHub = this.createHubProxy('countingHub'); 
        proxies.countingHub.client = { };
        proxies.countingHub.server = {
            send: function (n) {
            /// <summary>Calls the Send method on the server-side CountingHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"n\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.countingHub.invoke.apply(proxies.countingHub, $.merge(["Send"], $.makeArray(arguments)));
             }
        };

        proxies.demo = this.createHubProxy('demo'); 
        proxies.demo.client = { };
        proxies.demo.server = {
            addToGroups: function () {
            /// <summary>Calls the AddToGroups method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["AddToGroups"], $.makeArray(arguments)));
             },

            cancelledGenericTask: function () {
            /// <summary>Calls the CancelledGenericTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["CancelledGenericTask"], $.makeArray(arguments)));
             },

            cancelledTask: function () {
            /// <summary>Calls the CancelledTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["CancelledTask"], $.makeArray(arguments)));
             },

            complexArray: function (people) {
            /// <summary>Calls the ComplexArray method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"people\" type=\"Object\">Server side type is Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.DemoHub+Person[]</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["ComplexArray"], $.makeArray(arguments)));
             },

            complexType: function (p) {
            /// <summary>Calls the ComplexType method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"p\" type=\"Object\">Server side type is Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.DemoHub+Person</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["ComplexType"], $.makeArray(arguments)));
             },

            doSomethingAndCallError: function () {
            /// <summary>Calls the DoSomethingAndCallError method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["DoSomethingAndCallError"], $.makeArray(arguments)));
             },

            dynamicInvoke: function (method) {
            /// <summary>Calls the DynamicInvoke method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"method\" type=\"String\">Server side type is System.String</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["DynamicInvoke"], $.makeArray(arguments)));
             },

            dynamicTask: function () {
            /// <summary>Calls the DynamicTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["DynamicTask"], $.makeArray(arguments)));
             },

            genericTaskWithContinueWith: function () {
            /// <summary>Calls the GenericTaskWithContinueWith method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["GenericTaskWithContinueWith"], $.makeArray(arguments)));
             },

            genericTaskWithException: function () {
            /// <summary>Calls the GenericTaskWithException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["GenericTaskWithException"], $.makeArray(arguments)));
             },

            getValue: function () {
            /// <summary>Calls the GetValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["GetValue"], $.makeArray(arguments)));
             },

            inlineScriptTag: function () {
            /// <summary>Calls the InlineScriptTag method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["InlineScriptTag"], $.makeArray(arguments)));
             },

            mispelledClientMethod: function () {
            /// <summary>Calls the MispelledClientMethod method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["MispelledClientMethod"], $.makeArray(arguments)));
             },

            multipleCalls: function () {
            /// <summary>Calls the MultipleCalls method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["MultipleCalls"], $.makeArray(arguments)));
             },

            overload: function () {
            /// <summary>Calls the Overload method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["Overload"], $.makeArray(arguments)));
             },

            passingDynamicComplex: function (p) {
            /// <summary>Calls the PassingDynamicComplex method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"p\" type=\"Object\">Server side type is System.Object</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["PassingDynamicComplex"], $.makeArray(arguments)));
             },

            plainTask: function () {
            /// <summary>Calls the PlainTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["PlainTask"], $.makeArray(arguments)));
             },

            readAnyState: function () {
            /// <summary>Calls the ReadAnyState method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["ReadAnyState"], $.makeArray(arguments)));
             },

            readStateValue: function () {
            /// <summary>Calls the ReadStateValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["ReadStateValue"], $.makeArray(arguments)));
             },

            setStateValue: function (value) {
            /// <summary>Calls the SetStateValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"value\" type=\"String\">Server side type is System.String</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["SetStateValue"], $.makeArray(arguments)));
             },

            simpleArray: function (nums) {
            /// <summary>Calls the SimpleArray method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"nums\" type=\"Object\">Server side type is System.Int32[]</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["SimpleArray"], $.makeArray(arguments)));
             },

            synchronousException: function () {
            /// <summary>Calls the SynchronousException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["SynchronousException"], $.makeArray(arguments)));
             },

            taskWithException: function () {
            /// <summary>Calls the TaskWithException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["TaskWithException"], $.makeArray(arguments)));
             },

            testGuid: function () {
            /// <summary>Calls the TestGuid method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["TestGuid"], $.makeArray(arguments)));
             },

            unsupportedOverload: function (x) {
            /// <summary>Calls the UnsupportedOverload method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"x\" type=\"String\">Server side type is System.String</param>
                return proxies.demo.invoke.apply(proxies.demo, $.merge(["UnsupportedOverload"], $.makeArray(arguments)));
             }
        };

        proxies.DrawingPad = this.createHubProxy('DrawingPad'); 
        proxies.DrawingPad.client = { };
        proxies.DrawingPad.server = {
            Draw: function (data) {
            /// <summary>Calls the Draw method on the server-side DrawingPad hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"data\" type=\"Object\">Server side type is Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.DrawingPad.DrawingPad+Line</param>
                return proxies.DrawingPad.invoke.apply(proxies.DrawingPad, $.merge(["Draw"], $.makeArray(arguments)));
             },

            join: function () {
            /// <summary>Calls the Join method on the server-side DrawingPad hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.DrawingPad.invoke.apply(proxies.DrawingPad, $.merge(["Join"], $.makeArray(arguments)));
             }
        };

        proxies.headerAuthHub = this.createHubProxy('headerAuthHub'); 
        proxies.headerAuthHub.client = { };
        proxies.headerAuthHub.server = {
        };

        proxies.hubBench = this.createHubProxy('hubBench'); 
        proxies.hubBench.client = { };
        proxies.hubBench.server = {
            hitMe: function (start, clientCalls, connectionId) {
            /// <summary>Calls the HitMe method on the server-side HubBench hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"start\" type=\"Number\">Server side type is System.Int64</param>
            /// <param name=\"clientCalls\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
                return proxies.hubBench.invoke.apply(proxies.hubBench, $.merge(["HitMe"], $.makeArray(arguments)));
             },

            hitUs: function (start, clientCalls) {
            /// <summary>Calls the HitUs method on the server-side HubBench hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"start\" type=\"Number\">Server side type is System.Int64</param>
            /// <param name=\"clientCalls\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.hubBench.invoke.apply(proxies.hubBench, $.merge(["HitUs"], $.makeArray(arguments)));
             }
        };

        proxies.hubConnectionAPI = this.createHubProxy('hubConnectionAPI'); 
        proxies.hubConnectionAPI.client = { };
        proxies.hubConnectionAPI.server = {
            displayMessageAll: function (message) {
            /// <summary>Calls the DisplayMessageAll method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageAll"], $.makeArray(arguments)));
             },

            displayMessageAllExcept: function (message, excludeConnectionIds) {
            /// <summary>Calls the DisplayMessageAllExcept method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"excludeConnectionIds\" type=\"Object\">Server side type is System.String[]</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageAllExcept"], $.makeArray(arguments)));
             },

            displayMessageCaller: function (message) {
            /// <summary>Calls the DisplayMessageCaller method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageCaller"], $.makeArray(arguments)));
             },

            displayMessageGroup: function (groupName, message) {
            /// <summary>Calls the DisplayMessageGroup method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageGroup"], $.makeArray(arguments)));
             },

            displayMessageGroupExcept: function (groupName, message, excludeConnectionIds) {
            /// <summary>Calls the DisplayMessageGroupExcept method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"excludeConnectionIds\" type=\"Object\">Server side type is System.String[]</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageGroupExcept"], $.makeArray(arguments)));
             },

            displayMessageOther: function (message) {
            /// <summary>Calls the DisplayMessageOther method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageOther"], $.makeArray(arguments)));
             },

            displayMessageOthersInGroup: function (groupName, message) {
            /// <summary>Calls the DisplayMessageOthersInGroup method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageOthersInGroup"], $.makeArray(arguments)));
             },

            displayMessageSpecified: function (targetConnectionId, message) {
            /// <summary>Calls the DisplayMessageSpecified method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"targetConnectionId\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["DisplayMessageSpecified"], $.makeArray(arguments)));
             },

            joinGroup: function (connectionId, groupName) {
            /// <summary>Calls the JoinGroup method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["JoinGroup"], $.makeArray(arguments)));
             },

            leaveGroup: function (connectionId, groupName) {
            /// <summary>Calls the LeaveGroup method on the server-side HubConnectionAPI hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
                return proxies.hubConnectionAPI.invoke.apply(proxies.hubConnectionAPI, $.merge(["LeaveGroup"], $.makeArray(arguments)));
             }
        };

        proxies.incomingAuthHub = this.createHubProxy('incomingAuthHub'); 
        proxies.incomingAuthHub.client = { };
        proxies.incomingAuthHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side IncomingAuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.incomingAuthHub.invoke.apply(proxies.incomingAuthHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        proxies.inheritAuthHub = this.createHubProxy('inheritAuthHub'); 
        proxies.inheritAuthHub.client = { };
        proxies.inheritAuthHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side InheritAuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.inheritAuthHub.invoke.apply(proxies.inheritAuthHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        proxies.invokeAuthHub = this.createHubProxy('invokeAuthHub'); 
        proxies.invokeAuthHub.client = { };
        proxies.invokeAuthHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side InvokeAuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.invokeAuthHub.invoke.apply(proxies.invokeAuthHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        proxies.messageLoops = this.createHubProxy('messageLoops'); 
        proxies.messageLoops.client = { };
        proxies.messageLoops.server = {
            joinGroup: function (connectionId, groupName) {
            /// <summary>Calls the JoinGroup method on the server-side MessageLoops hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
                return proxies.messageLoops.invoke.apply(proxies.messageLoops, $.merge(["JoinGroup"], $.makeArray(arguments)));
             },

            leaveGroup: function (connectionId, groupName) {
            /// <summary>Calls the LeaveGroup method on the server-side MessageLoops hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
                return proxies.messageLoops.invoke.apply(proxies.messageLoops, $.merge(["LeaveGroup"], $.makeArray(arguments)));
             },

            sendMessageCountToAll: function (messageCount, sleepTime) {
            /// <summary>Calls the SendMessageCountToAll method on the server-side MessageLoops hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"messageCount\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"sleepTime\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.messageLoops.invoke.apply(proxies.messageLoops, $.merge(["SendMessageCountToAll"], $.makeArray(arguments)));
             },

            sendMessageCountToCaller: function (messageCount, sleepTime) {
            /// <summary>Calls the SendMessageCountToCaller method on the server-side MessageLoops hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"messageCount\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"sleepTime\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.messageLoops.invoke.apply(proxies.messageLoops, $.merge(["SendMessageCountToCaller"], $.makeArray(arguments)));
             },

            sendMessageCountToGroup: function (messageCount, groupName, sleepTime) {
            /// <summary>Calls the SendMessageCountToGroup method on the server-side MessageLoops hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"messageCount\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"sleepTime\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.messageLoops.invoke.apply(proxies.messageLoops, $.merge(["SendMessageCountToGroup"], $.makeArray(arguments)));
             }
        };

        proxies.mouseTracking = this.createHubProxy('mouseTracking'); 
        proxies.mouseTracking.client = { };
        proxies.mouseTracking.server = {
            join: function () {
            /// <summary>Calls the Join method on the server-side MouseTracking hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.mouseTracking.invoke.apply(proxies.mouseTracking, $.merge(["Join"], $.makeArray(arguments)));
             },

            move: function (x, y) {
            /// <summary>Calls the Move method on the server-side MouseTracking hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"x\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"y\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.mouseTracking.invoke.apply(proxies.mouseTracking, $.merge(["Move"], $.makeArray(arguments)));
             }
        };

        proxies.noAuthHub = this.createHubProxy('noAuthHub'); 
        proxies.noAuthHub.client = { };
        proxies.noAuthHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side NoAuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.noAuthHub.invoke.apply(proxies.noAuthHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        proxies.realtime = this.createHubProxy('realtime'); 
        proxies.realtime.client = { };
        proxies.realtime.server = {
            getFPS: function () {
            /// <summary>Calls the GetFPS method on the server-side Realtime hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.realtime.invoke.apply(proxies.realtime, $.merge(["GetFPS"], $.makeArray(arguments)));
             },

            getFrameId: function () {
            /// <summary>Calls the GetFrameId method on the server-side Realtime hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.realtime.invoke.apply(proxies.realtime, $.merge(["GetFrameId"], $.makeArray(arguments)));
             },

            isEngineRunning: function () {
            /// <summary>Calls the IsEngineRunning method on the server-side Realtime hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.realtime.invoke.apply(proxies.realtime, $.merge(["IsEngineRunning"], $.makeArray(arguments)));
             },

            setFPS: function (fps) {
            /// <summary>Calls the SetFPS method on the server-side Realtime hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"fps\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.realtime.invoke.apply(proxies.realtime, $.merge(["SetFPS"], $.makeArray(arguments)));
             },

            start: function () {
            /// <summary>Calls the Start method on the server-side Realtime hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.realtime.invoke.apply(proxies.realtime, $.merge(["Start"], $.makeArray(arguments)));
             },

            stop: function () {
            /// <summary>Calls the Stop method on the server-side Realtime hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.realtime.invoke.apply(proxies.realtime, $.merge(["Stop"], $.makeArray(arguments)));
             }
        };

        proxies.shapeShare = this.createHubProxy('shapeShare'); 
        proxies.shapeShare.client = { };
        proxies.shapeShare.server = {
            changeShape: function (id, x, y, w, h) {
            /// <summary>Calls the ChangeShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"id\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"x\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"y\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"w\" type=\"Number\">Server side type is System.Int32</param>
            /// <param name=\"h\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["ChangeShape"], $.makeArray(arguments)));
             },

            changeUserName: function (currentUserName, newUserName) {
            /// <summary>Calls the ChangeUserName method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"currentUserName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"newUserName\" type=\"String\">Server side type is System.String</param>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["ChangeUserName"], $.makeArray(arguments)));
             },

            createShape: function (type) {
            /// <summary>Calls the CreateShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"type\" type=\"String\">Server side type is System.String</param>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["CreateShape"], $.makeArray(arguments)));
             },

            deleteAllShapes: function () {
            /// <summary>Calls the DeleteAllShapes method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["DeleteAllShapes"], $.makeArray(arguments)));
             },

            deleteShape: function (id) {
            /// <summary>Calls the DeleteShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"id\" type=\"String\">Server side type is System.String</param>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["DeleteShape"], $.makeArray(arguments)));
             },

            getShapes: function () {
            /// <summary>Calls the GetShapes method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["GetShapes"], $.makeArray(arguments)));
             },

            join: function (userName) {
            /// <summary>Calls the Join method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"userName\" type=\"String\">Server side type is System.String</param>
                return proxies.shapeShare.invoke.apply(proxies.shapeShare, $.merge(["Join"], $.makeArray(arguments)));
             }
        };

        proxies.StatusHub = this.createHubProxy('StatusHub'); 
        proxies.StatusHub.client = { };
        proxies.StatusHub.server = {
            ping: function () {
            /// <summary>Calls the Ping method on the server-side StatusHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.StatusHub.invoke.apply(proxies.StatusHub, $.merge(["Ping"], $.makeArray(arguments)));
             }
        };

        proxies.stressHub = this.createHubProxy('stressHub'); 
        proxies.stressHub.client = { };
        proxies.stressHub.server = {
            echoToCaller: function (message) {
            /// <summary>Calls the EchoToCaller method on the server-side StressHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.stressHub.invoke.apply(proxies.stressHub, $.merge(["EchoToCaller"], $.makeArray(arguments)));
             },

            echoToGroup: function (groupName, message) {
            /// <summary>Calls the EchoToGroup method on the server-side StressHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"Number\">Server side type is System.Int32</param>
                return proxies.stressHub.invoke.apply(proxies.stressHub, $.merge(["EchoToGroup"], $.makeArray(arguments)));
             },

            joinGroup: function (groupName, connectionId) {
            /// <summary>Calls the JoinGroup method on the server-side StressHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
                return proxies.stressHub.invoke.apply(proxies.stressHub, $.merge(["JoinGroup"], $.makeArray(arguments)));
             }
        };

        proxies.testHub = this.createHubProxy('testHub'); 
        proxies.testHub.client = { };
        proxies.testHub.server = {
            joinGroup: function (groupName, connectionId) {
            /// <summary>Calls the JoinGroup method on the server-side TestHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
                return proxies.testHub.invoke.apply(proxies.testHub, $.merge(["JoinGroup"], $.makeArray(arguments)));
             },

            leaveGroup: function (groupName, connectionId) {
            /// <summary>Calls the LeaveGroup method on the server-side TestHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"connectionId\" type=\"String\">Server side type is System.String</param>
                return proxies.testHub.invoke.apply(proxies.testHub, $.merge(["LeaveGroup"], $.makeArray(arguments)));
             },

            sendToAll: function (message) {
            /// <summary>Calls the SendToAll method on the server-side TestHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.testHub.invoke.apply(proxies.testHub, $.merge(["SendToAll"], $.makeArray(arguments)));
             },

            sendToCaller: function (message) {
            /// <summary>Calls the SendToCaller method on the server-side TestHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.testHub.invoke.apply(proxies.testHub, $.merge(["SendToCaller"], $.makeArray(arguments)));
             },

            sendToClient: function (targetConnectionId, message) {
            /// <summary>Calls the SendToClient method on the server-side TestHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"targetConnectionId\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.testHub.invoke.apply(proxies.testHub, $.merge(["SendToClient"], $.makeArray(arguments)));
             },

            sendToGroup: function (groupName, message) {
            /// <summary>Calls the SendToGroup method on the server-side TestHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name=\"groupName\" type=\"String\">Server side type is System.String</param>
            /// <param name=\"message\" type=\"String\">Server side type is System.String</param>
                return proxies.testHub.invoke.apply(proxies.testHub, $.merge(["SendToGroup"], $.makeArray(arguments)));
             }
        };

        proxies.userAndRoleAuthHub = this.createHubProxy('userAndRoleAuthHub'); 
        proxies.userAndRoleAuthHub.client = { };
        proxies.userAndRoleAuthHub.server = {
            invokedFromClient: function () {
            /// <summary>Calls the InvokedFromClient method on the server-side UserAndRoleAuthHub hub.&#10;Returns a jQuery.Deferred() promise.</summary>
                return proxies.userAndRoleAuthHub.invoke.apply(proxies.userAndRoleAuthHub, $.merge(["InvokedFromClient"], $.makeArray(arguments)));
             }
        };

        return proxies;
    };

    signalR.hub = $.hubConnection("/signalr", { useDefaultPath: false });
    $.extend(signalR, signalR.hub.createHubProxies());

}(window.jQuery, window));