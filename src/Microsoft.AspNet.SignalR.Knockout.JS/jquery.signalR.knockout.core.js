// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="../Microsoft.AspNet.SignalR.Client.JS/jquery.signalR.hubs.js" />
/// <reference path="jquery.signalR.knockout.diff.js" />

(function ($, undefined) {
    "use strict";

    var utils = $.signalR.knockout._utils,
        ko = window.ko,
        savedCreateHubProxy = $.hubConnection.prototype.createHubProxy,
        defaultBuilders = [{
            name: "koReadOnlyObservable",
            diffable: true,
            match: function (object) {
                return ko.isObservable(object) && !ko.isWriteableObservable(object);
            },
            peek: utils.koPeek,
            create: utils.identity,
            update: utils.identity
        }, {
            name: "koObservableArray",
            diffable: true,
            match: function (object) {
                return ko.isObservable(object) && object.indexOf;
            },
            peek: utils.koPeek,
            create: ko.observableArray,
            update: utils.koUpdate
        }, {
            name: "koObservable",
            diffable: true,
            match: ko.isObservable,
            peek: utils.koPeek,
            create: ko.observable,
            update: utils.koUpdate
        }, {
            name: "escape",
            diffable: true,
            match: utils.getTag,
            peek: utils.identity,
            create: utils.identity
        }];


    function subscribe(model, diffTools, callback, options) {
        var diffBase = {},
            throttle = options.throttle || 0,
            callbackTimeout = null;

        function executeCallback() {
            // Default to clearing diff if callback doesn't explicitly return false for failure
            // TODO: Allow async callback.
            if (callback(diffBase) !== false) {
                diffBase = {};
            }

            callbackTimeout = null;
        }

        function subscribeToChildren(parent, nestedProps) {
            var subscriptions = [],
                parentBuilder = diffTools.builderOf(parent);

            if (parentBuilder) {
                // nestedProps represents the property chain used to access the part of the diff
                // corresponding to the child. The diff will have an extra "value" prop in chain
                // for each tagged value. This allows us to know what is actually being tagged.
                nestedProps = nestedProps.concat([["value", { _tag: parentBuilder.name }]]);
                parent = parentBuilder.peek(parent);
            }

            utils.eachPublic(parent, function (key, value) {
                $.merge(subscriptions, subscribeToChild(value, nestedProps.concat([key])));
            });

            return subscriptions;
        }

        function subscribeToChild(child, nestedProps) {
            var childSubscription,
                nestedSubscriptions = [],
                childBuilder = diffTools.builderOf(child),
                taggedChildBefore;

            function disposeNestedSubscriptons() {
                $.each(nestedSubscriptions, function (_, subscription) {
                    subscription.before.dispose();
                    subscription.after.dispose();
                });
                nestedSubscriptions = [];
            }

            if (ko.isSubscribable(child)) {
                childSubscription = {};

                childSubscription.before = child.subscribe(function () {
                    if (!options.disableSubscriptions && childBuilder.diffable !== false) {
                        taggedChildBefore = diffTools.tag(child);
                    }
                }, undefined, "beforeChange");

                childSubscription.after = child.subscribe(function () {
                    var childDiff = diffBase,
                        prop,
                        taggedChildAfter,
                        newDiff;

                    // Don't want to repost other's updates
                    if (!options.disableSubscriptions) {
                        // ensure parent objects in diff
                        for (var i = 0; i < nestedProps.length; i++) {
                            prop = nestedProps[i];

                            // If prop is the "value" part of a tagged object, add the _tag prop
                            if ($.isArray(prop)) {
                                $.extend(childDiff, prop[1]);
                                prop = prop[0];
                            }

                            if (!childDiff.hasOwnProperty(prop)) {
                                childDiff[prop] = {};
                            }
                            childDiff = childDiff[prop];
                        }
                        
                        taggedChildAfter = diffTools.tag(child);

                        // We don't always get notified before a change. If that's the case,
                        // send everything!
                        if (taggedChildBefore === undefined) {
                            $.extend(true, childDiff, taggedChildAfter);
                            childDiff._updated = true;
                            childDiff._replaced = true;
                        } else {
                            newDiff = diffTools.diff(taggedChildBefore, taggedChildAfter);

                            // There might not actually be any changes! Who'd have thunk it?
                            if (newDiff.hasOwnProperty("value") || !utils.getTag(newDiff)) {
                                $.extend(true, childDiff, newDiff);
                                childDiff._updated = true;
                            }

                            taggedChildBefore = undefined;
                        }

                        if (callbackTimeout === null) {
                            callbackTimeout = window.setTimeout(executeCallback, throttle);
                        }
                    }

                    // Stuff's changed! Better make sure we are correctly subscribed to it all.
                    disposeNestedSubscriptons();
                    subscribeToChildren(child, nestedProps);
                });
            }

            nestedSubscriptions = subscribeToChildren(child, nestedProps);
            return childSubscription === undefined ?
                nestedSubscriptions : [childSubscription].concat(nestedSubscriptions);
        }

        return subscribeToChild(model, []);
    }

    function registerObservable(proxy, model, options) {
        var opts = options || {},
            builders = opts.builders || [],
            diffTools;

        builders = builders.concat(defaultBuilders);
        diffTools = new $.signalR.knockout.diffTools(builders);

        function attach() {
            function isConnected() {
                return proxy.connection.state === $.signalR.connectionState.connected;
            }

            function getFullState() {
                if (isConnected()) {
                    proxy.invoke("GetKnockoutState");
                }
            }

            function updateServer(diff) {
                if (isConnected()) {
                    proxy.invoke("OnKnockoutUpdate", diff);
                    return true;
                }
                return false;
            }

            model = model || ko.dataFor(window.document.body);

            if (model === undefined) {
                throw new Error("SignalR-Knockout: Could not find view model. Please ensure one is attached to the document body or is passed to hubProxy.observable().");
            }

            proxy.on("OnKnockoutUpdate", function (diff) {
                opts.disableSubscriptions = true;
                diffTools.merge(model, diff);
                opts.disableSubscriptions = false;
            });

            proxy.connection.stateChanged(getFullState);
            getFullState();

            subscribe(model, diffTools, updateServer, opts);
        }

        // DOM must be ready for ko.dataFor(window.document.body) to work
        if (model) {
            attach();
        } else {
            $(attach);
        }
    }

    // Hopefully monkey patching works no one needs to go into $.signalR.knockout,
    // but making these accessible eases testing. Who knows? Someone might find it useful.
    $.signalR.knockout._defaultBuilders = defaultBuilders;
    $.signalR.knockout.subscribe = subscribe;
    $.signalR.knockout.registerObservable = registerObservable;

    // Monkey patch!
    $.hubConnection.prototype.createHubProxy = function (hubName) {
        var proxy = savedCreateHubProxy.apply(this, arguments);
        proxy.observable = $.proxy(registerObservable, undefined, proxy);
        return proxy;
    };
}(window.jQuery));
