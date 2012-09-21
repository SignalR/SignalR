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
            // Call the client hub method
            callback.apply(hub, $.makeArray(arguments));
        };
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

    signalR.hub = $.hubConnection("/signalr")
        .starting(function () {
            createHubProxies(signalR, this);
        });

    signalR.chat = signalR.hub.createProxy('chat'); 
    signalR.chat.client = { };
    signalR.chat.server = {
        join: function () {
            /// <summary>Calls the Join method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.chat.invoke.apply(signalR.chat, $.merge(["Join"], $.makeArray(arguments)));
         },

        send: function (content) {
            /// <summary>Calls the Send method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="content" type="String">Server side type is System.String</param>
            return signalR.chat.invoke.apply(signalR.chat, $.merge(["Send"], $.makeArray(arguments)));
         },

        getUsers: function () {
            /// <summary>Calls the GetUsers method on the server-side Chat hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.chat.invoke.apply(signalR.chat, $.merge(["GetUsers"], $.makeArray(arguments)));
         }
    };

    signalR.demo = signalR.hub.createProxy('demo'); 
    signalR.demo.client = { };
    signalR.demo.server = {
        getValue: function () {
            /// <summary>Calls the GetValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["GetValue"], $.makeArray(arguments)));
         },

        addToGroups: function () {
            /// <summary>Calls the AddToGroups method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["AddToGroups"], $.makeArray(arguments)));
         },

        doSomethingAndCallError: function () {
            /// <summary>Calls the DoSomethingAndCallError method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["DoSomethingAndCallError"], $.makeArray(arguments)));
         },

        dynamicTask: function () {
            /// <summary>Calls the DynamicTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["DynamicTask"], $.makeArray(arguments)));
         },

        plainTask: function () {
            /// <summary>Calls the PlainTask method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["PlainTask"], $.makeArray(arguments)));
         },

        genericTaskTypedAsPlain: function () {
            /// <summary>Calls the GenericTaskTypedAsPlain method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["GenericTaskTypedAsPlain"], $.makeArray(arguments)));
         },

        taskWithException: function () {
            /// <summary>Calls the TaskWithException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["TaskWithException"], $.makeArray(arguments)));
         },

        genericTaskWithException: function () {
            /// <summary>Calls the GenericTaskWithException method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["GenericTaskWithException"], $.makeArray(arguments)));
         },

        simpleArray: function (nums) {
            /// <summary>Calls the SimpleArray method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="nums" type="Object">Server side type is System.Int32[]</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["SimpleArray"], $.makeArray(arguments)));
         },

        readStateValue: function () {
            /// <summary>Calls the ReadStateValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["ReadStateValue"], $.makeArray(arguments)));
         },

        setStateValue: function (value) {
            /// <summary>Calls the SetStateValue method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="value" type="String">Server side type is System.String</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["SetStateValue"], $.makeArray(arguments)));
         },

        complexArray: function (people) {
            /// <summary>Calls the ComplexArray method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="people" type="Object">Server side type is SignalR.Samples.Hubs.DemoHub.DemoHub+Person[]</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["ComplexArray"], $.makeArray(arguments)));
         },

        complexType: function (p) {
            /// <summary>Calls the ComplexType method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="p" type="Object">Server side type is SignalR.Samples.Hubs.DemoHub.DemoHub+Person</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["ComplexType"], $.makeArray(arguments)));
         },

        passingDynamicComplex: function (p) {
            /// <summary>Calls the PassingDynamicComplex method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="p" type="Object">Server side type is System.Object</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["PassingDynamicComplex"], $.makeArray(arguments)));
         },

        multipleCalls: function () {
            /// <summary>Calls the MultipleCalls method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["MultipleCalls"], $.makeArray(arguments)));
         },

        overload: function () {
            /// <summary>Calls the Overload method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["Overload"], $.makeArray(arguments)));
         },

        unsupportedOverload: function (x) {
            /// <summary>Calls the UnsupportedOverload method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="x" type="String">Server side type is System.String</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["UnsupportedOverload"], $.makeArray(arguments)));
         },

        testGuid: function () {
            /// <summary>Calls the TestGuid method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["TestGuid"], $.makeArray(arguments)));
         },

        dynamicInvoke: function (method) {
            /// <summary>Calls the DynamicInvoke method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="method" type="String">Server side type is System.String</param>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["DynamicInvoke"], $.makeArray(arguments)));
         },

        mispelledClientMethod: function () {
            /// <summary>Calls the MispelledClientMethod method on the server-side demo hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.demo.invoke.apply(signalR.demo, $.merge(["MispelledClientMethod"], $.makeArray(arguments)));
         }
    };

    signalR.DrawingPad = signalR.hub.createProxy('DrawingPad'); 
    signalR.DrawingPad.client = { };
    signalR.DrawingPad.server = {
        join: function () {
            /// <summary>Calls the Join method on the server-side DrawingPad hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.DrawingPad.invoke.apply(signalR.DrawingPad, $.merge(["Join"], $.makeArray(arguments)));
         },

        DrawALine: function (data) {
            /// <summary>Calls the DrawALine method on the server-side DrawingPad hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="data" type="Object">Server side type is SignalR.Hosting.AspNet.Samples.Hubs.DrawingPad.DrawingPad+Line</param>
            return signalR.DrawingPad.invoke.apply(signalR.DrawingPad, $.merge(["DrawALine"], $.makeArray(arguments)));
         }
    };

    signalR.hubBench = signalR.hub.createProxy('hubBench'); 
    signalR.hubBench.client = { };
    signalR.hubBench.server = {
        hitMe: function (start, clientCalls, connectionId) {
            /// <summary>Calls the HitMe method on the server-side HubBench hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="start" type="Number">Server side type is System.Int64</param>
            /// <param name="clientCalls" type="Number">Server side type is System.Int32</param>
            /// <param name="connectionId" type="String">Server side type is System.String</param>
            return signalR.hubBench.invoke.apply(signalR.hubBench, $.merge(["HitMe"], $.makeArray(arguments)));
         },

        hitUs: function (start, clientCalls) {
            /// <summary>Calls the HitUs method on the server-side HubBench hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="start" type="Number">Server side type is System.Int64</param>
            /// <param name="clientCalls" type="Number">Server side type is System.Int32</param>
            return signalR.hubBench.invoke.apply(signalR.hubBench, $.merge(["HitUs"], $.makeArray(arguments)));
         }
    };

    signalR.mouseTracking = signalR.hub.createProxy('mouseTracking'); 
    signalR.mouseTracking.client = { };
    signalR.mouseTracking.server = {
        join: function () {
            /// <summary>Calls the Join method on the server-side MouseTracking hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.mouseTracking.invoke.apply(signalR.mouseTracking, $.merge(["Join"], $.makeArray(arguments)));
         },

        move: function (x, y) {
            /// <summary>Calls the Move method on the server-side MouseTracking hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="x" type="Number">Server side type is System.Int32</param>
            /// <param name="y" type="Number">Server side type is System.Int32</param>
            return signalR.mouseTracking.invoke.apply(signalR.mouseTracking, $.merge(["Move"], $.makeArray(arguments)));
         }
    };

    signalR.shapeShare = signalR.hub.createProxy('shapeShare'); 
    signalR.shapeShare.client = { };
    signalR.shapeShare.server = {
        getShapes: function () {
            /// <summary>Calls the GetShapes method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["GetShapes"], $.makeArray(arguments)));
         },

        join: function (userName) {
            /// <summary>Calls the Join method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="userName" type="String">Server side type is System.String</param>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["Join"], $.makeArray(arguments)));
         },

        changeUserName: function (currentUserName, newUserName) {
            /// <summary>Calls the ChangeUserName method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="currentUserName" type="String">Server side type is System.String</param>
            /// <param name="newUserName" type="String">Server side type is System.String</param>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["ChangeUserName"], $.makeArray(arguments)));
         },

        createShape: function (type) {
            /// <summary>Calls the CreateShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="type" type="String">Server side type is System.String</param>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["CreateShape"], $.makeArray(arguments)));
         },

        changeShape: function (id, x, y, w, h) {
            /// <summary>Calls the ChangeShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="id" type="String">Server side type is System.String</param>
            /// <param name="x" type="Number">Server side type is System.Int32</param>
            /// <param name="y" type="Number">Server side type is System.Int32</param>
            /// <param name="w" type="Number">Server side type is System.Int32</param>
            /// <param name="h" type="Number">Server side type is System.Int32</param>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["ChangeShape"], $.makeArray(arguments)));
         },

        deleteShape: function (id) {
            /// <summary>Calls the DeleteShape method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            /// <param name="id" type="String">Server side type is System.String</param>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["DeleteShape"], $.makeArray(arguments)));
         },

        deleteAllShapes: function () {
            /// <summary>Calls the DeleteAllShapes method on the server-side ShapeShare hub.&#10;Returns a jQuery.Deferred() promise.</summary>
            return signalR.shapeShare.invoke.apply(signalR.shapeShare, $.merge(["DeleteAllShapes"], $.makeArray(arguments)));
         }
    };

    signalR.StatusHub = signalR.hub.createProxy('StatusHub'); 
    signalR.StatusHub.client = { };
    signalR.StatusHub.server = {
    };

}(window.jQuery, window));