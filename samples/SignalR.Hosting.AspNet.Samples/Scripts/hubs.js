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
        chat: {
            _: {
                hubName: 'Chat',
                ignoreMembers: ['getUsers', 'join', 'send', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            join: function (callback) {
                return serverCall(this, "Join", $.makeArray(arguments));
            },

            send: function (content, callback) {
                return serverCall(this, "Send", $.makeArray(arguments));
            },

            getUsers: function (callback) {
                return serverCall(this, "GetUsers", $.makeArray(arguments));
            }
        },
        demo: {
            _: {
                hubName: 'demo',
                ignoreMembers: ['addToGroups', 'complexArray', 'complexType', 'doSomethingAndCallError', 'dynamicTask', 'genericTaskTypedAsPlain', 'genericTaskWithException', 'getValue', 'multipleCalls', 'overload', 'passingDynamicComplex', 'plainTask', 'readStateValue', 'setStateValue', 'simpleArray', 'taskWithException', 'unsupportedOverload', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            getValue: function (callback) {
                return serverCall(this, "GetValue", $.makeArray(arguments));
            },

            addToGroups: function (callback) {
                return serverCall(this, "AddToGroups", $.makeArray(arguments));
            },

            doSomethingAndCallError: function (callback) {
                return serverCall(this, "DoSomethingAndCallError", $.makeArray(arguments));
            },

            dynamicTask: function (callback) {
                return serverCall(this, "DynamicTask", $.makeArray(arguments));
            },

            plainTask: function (callback) {
                return serverCall(this, "PlainTask", $.makeArray(arguments));
            },

            genericTaskTypedAsPlain: function (callback) {
                return serverCall(this, "GenericTaskTypedAsPlain", $.makeArray(arguments));
            },

            taskWithException: function (callback) {
                return serverCall(this, "TaskWithException", $.makeArray(arguments));
            },

            genericTaskWithException: function (callback) {
                return serverCall(this, "GenericTaskWithException", $.makeArray(arguments));
            },

            simpleArray: function (nums, callback) {
                return serverCall(this, "SimpleArray", $.makeArray(arguments));
            },

            readStateValue: function (callback) {
                return serverCall(this, "ReadStateValue", $.makeArray(arguments));
            },

            setStateValue: function (value, callback) {
                return serverCall(this, "SetStateValue", $.makeArray(arguments));
            },

            complexArray: function (people, callback) {
                return serverCall(this, "ComplexArray", $.makeArray(arguments));
            },

            complexType: function (p, callback) {
                return serverCall(this, "ComplexType", $.makeArray(arguments));
            },

            passingDynamicComplex: function (p, callback) {
                return serverCall(this, "PassingDynamicComplex", $.makeArray(arguments));
            },

            multipleCalls: function (callback) {
                return serverCall(this, "MultipleCalls", $.makeArray(arguments));
            },

            overload: function (callback) {
                return serverCall(this, "Overload", $.makeArray(arguments));
            },

            unsupportedOverload: function (x, callback) {
                return serverCall(this, "UnsupportedOverload", $.makeArray(arguments));
            }
        },
        drawingPad: {
            _: {
                hubName: 'DrawingPad',
                ignoreMembers: ['drawLine', 'join', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            join: function (callback) {
                return serverCall(this, "Join", $.makeArray(arguments));
            },

            drawLine: function (data, callback) {
                return serverCall(this, "DrawLine", $.makeArray(arguments));
            }
        },
        hubBench: {
            _: {
                hubName: 'HubBench',
                ignoreMembers: ['hitMe', 'hitUs', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            hitMe: function (clientCalls, connectionId, start, callback) {
                return serverCall(this, "HitMe", $.makeArray(arguments));
            },

            hitUs: function (clientCalls, start, callback) {
                return serverCall(this, "HitUs", $.makeArray(arguments));
            }
        },
        mouseTracking: {
            _: {
                hubName: 'MouseTracking',
                ignoreMembers: ['join', 'move', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            join: function (callback) {
                return serverCall(this, "Join", $.makeArray(arguments));
            },

            move: function (x, y, callback) {
                return serverCall(this, "Move", $.makeArray(arguments));
            }
        },
        shapeShare: {
            _: {
                hubName: 'ShapeShare',
                ignoreMembers: ['changeShape', 'changeUserName', 'createShape', 'deleteAllShapes', 'deleteShape', 'getShapes', 'join', 'namespace', 'ignoreMembers', 'callbacks'],
                connection: function () { return signalR.hub; }
            },

            getShapes: function (callback) {
                return serverCall(this, "GetShapes", $.makeArray(arguments));
            },

            join: function (userName, callback) {
                return serverCall(this, "Join", $.makeArray(arguments));
            },

            changeUserName: function (currentUserName, newUserName, callback) {
                return serverCall(this, "ChangeUserName", $.makeArray(arguments));
            },

            createShape: function (type, callback) {
                return serverCall(this, "CreateShape", $.makeArray(arguments));
            },

            changeShape: function (h, id, w, x, y, callback) {
                return serverCall(this, "ChangeShape", $.makeArray(arguments));
            },

            deleteShape: function (id, callback) {
                return serverCall(this, "DeleteShape", $.makeArray(arguments));
            },

            deleteAllShapes: function (callback) {
                return serverCall(this, "DeleteAllShapes", $.makeArray(arguments));
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

    signalR.hub = signalR("/signalr")
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