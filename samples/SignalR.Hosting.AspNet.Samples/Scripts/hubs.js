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
        chat: {
            _: {
                hubName: 'Chat',
                ignoreMembers: ['getUsers', 'join', 'send', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            join: function (callback) {
                return invoke(this, "Join", $.makeArray(arguments));
            },

            send: function (content, callback) {
                return invoke(this, "Send", $.makeArray(arguments));
            },

            getUsers: function (callback) {
                return invoke(this, "GetUsers", $.makeArray(arguments));
            }
        },
        demo: {
            _: {
                hubName: 'demo',
                ignoreMembers: ['addToGroups', 'complexArray', 'complexType', 'doSomethingAndCallError', 'dynamicInvoke', 'dynamicTask', 'genericTaskTypedAsPlain', 'genericTaskWithException', 'getValue', 'multipleCalls', 'overload', 'passingDynamicComplex', 'plainTask', 'readStateValue', 'setStateValue', 'simpleArray', 'taskWithException', 'testGuid', 'unsupportedOverload', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            getValue: function (callback) {
                return invoke(this, "GetValue", $.makeArray(arguments));
            },

            addToGroups: function (callback) {
                return invoke(this, "AddToGroups", $.makeArray(arguments));
            },

            doSomethingAndCallError: function (callback) {
                return invoke(this, "DoSomethingAndCallError", $.makeArray(arguments));
            },

            dynamicTask: function (callback) {
                return invoke(this, "DynamicTask", $.makeArray(arguments));
            },

            plainTask: function (callback) {
                return invoke(this, "PlainTask", $.makeArray(arguments));
            },

            genericTaskTypedAsPlain: function (callback) {
                return invoke(this, "GenericTaskTypedAsPlain", $.makeArray(arguments));
            },

            taskWithException: function (callback) {
                return invoke(this, "TaskWithException", $.makeArray(arguments));
            },

            genericTaskWithException: function (callback) {
                return invoke(this, "GenericTaskWithException", $.makeArray(arguments));
            },

            simpleArray: function (nums, callback) {
                return invoke(this, "SimpleArray", $.makeArray(arguments));
            },

            readStateValue: function (callback) {
                return invoke(this, "ReadStateValue", $.makeArray(arguments));
            },

            setStateValue: function (value, callback) {
                return invoke(this, "SetStateValue", $.makeArray(arguments));
            },

            complexArray: function (people, callback) {
                return invoke(this, "ComplexArray", $.makeArray(arguments));
            },

            complexType: function (p, callback) {
                return invoke(this, "ComplexType", $.makeArray(arguments));
            },

            passingDynamicComplex: function (p, callback) {
                return invoke(this, "PassingDynamicComplex", $.makeArray(arguments));
            },

            multipleCalls: function (callback) {
                return invoke(this, "MultipleCalls", $.makeArray(arguments));
            },

            overload: function (callback) {
                return invoke(this, "Overload", $.makeArray(arguments));
            },

            unsupportedOverload: function (x, callback) {
                return invoke(this, "UnsupportedOverload", $.makeArray(arguments));
            },

            testGuid: function (callback) {
                return invoke(this, "TestGuid", $.makeArray(arguments));
            },

            dynamicInvoke: function (method, callback) {
                return invoke(this, "DynamicInvoke", $.makeArray(arguments));
            }
        },
        drawingPad: {
            _: {
                hubName: 'DrawingPad',
                ignoreMembers: ['drawLine', 'join', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            join: function (callback) {
                return invoke(this, "Join", $.makeArray(arguments));
            },

            drawLine: function (data, callback) {
                return invoke(this, "DrawLine", $.makeArray(arguments));
            }
        },
        hubBench: {
            _: {
                hubName: 'HubBench',
                ignoreMembers: ['hitMe', 'hitUs', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            hitMe: function (clientCalls, connectionId, start, callback) {
                return invoke(this, "HitMe", $.makeArray(arguments));
            },

            hitUs: function (clientCalls, start, callback) {
                return invoke(this, "HitUs", $.makeArray(arguments));
            }
        },
        mouseTracking: {
            _: {
                hubName: 'MouseTracking',
                ignoreMembers: ['join', 'move', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            join: function (callback) {
                return invoke(this, "Join", $.makeArray(arguments));
            },

            move: function (x, y, callback) {
                return invoke(this, "Move", $.makeArray(arguments));
            }
        },
        shapeShare: {
            _: {
                hubName: 'ShapeShare',
                ignoreMembers: ['changeShape', 'changeUserName', 'createShape', 'deleteAllShapes', 'deleteShape', 'getShapes', 'join', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            getShapes: function (callback) {
                return invoke(this, "GetShapes", $.makeArray(arguments));
            },

            join: function (userName, callback) {
                return invoke(this, "Join", $.makeArray(arguments));
            },

            changeUserName: function (currentUserName, newUserName, callback) {
                return invoke(this, "ChangeUserName", $.makeArray(arguments));
            },

            createShape: function (type, callback) {
                return invoke(this, "CreateShape", $.makeArray(arguments));
            },

            changeShape: function (h, id, w, x, y, callback) {
                return invoke(this, "ChangeShape", $.makeArray(arguments));
            },

            deleteShape: function (id, callback) {
                return invoke(this, "DeleteShape", $.makeArray(arguments));
            },

            deleteAllShapes: function (callback) {
                return invoke(this, "DeleteAllShapes", $.makeArray(arguments));
            }
        },
        status: {
            _: {
                hubName: 'Status',
                ignoreMembers: ['namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            }

        }
    });

    signalR.hub = $.hubConnection("/signalr")
        .starting(function () {
            createHubProxies(signalR, this);
        });

}(window.jQuery, window));