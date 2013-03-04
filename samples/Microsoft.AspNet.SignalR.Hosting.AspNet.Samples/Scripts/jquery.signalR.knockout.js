/* jquery.signalR.knockout.utils.js */
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="../Microsoft.AspNet.SignalR.Client.JS/Scripts/jquery-1.6.4.js" />
/// <reference path="Scripts/knockout-2.2.1.debug.js" />

(function ($, undefined) {
    "use strict";

    // Utils is loaded first so we check if everything exists here and set up the "namespace"
    if (typeof ($) !== "function" || typeof ($.signalR) !== "function") {
        throw new Error("SignalR-Knockout: SignalR not found. Please ensure signalR.js is referenced before the signalR.knockout.js file.");
    }

    var signalR = $.signalR,
        utils;

    signalR.knockout = {};
    signalR.knockout._utils = utils = {
        koPeek: function (x) {
            // Maybe we could call x(), but I don't want to create an unintended dependency
            return x.peek();
        },

        koUpdate: function (x, newValue) {
            return x(newValue);
        },

        identity: function (x) {
            return x;
        },

        isPrimitive: function (value) {
            return (typeof value !== "object" && typeof value !== "function") || value === null;
        },

        getTag: function (value) {
            return utils.isPrimitive(value) ? undefined : value._tag;
        },

        eachPublic: function (object, f) {
            if (utils.isPrimitive(object)) {
                return object;
            }

            return $.each(object, function (key, value) {
                // Don't iterate over properties that are conventionally "private"
                if (typeof key === "string" && key[0] === "_") {
                    return;
                }

                f(key, value);
            });
        },

        mapPublic: function (object, f) {
            if (utils.isPrimitive(object)) {
                return object;
            }

            var copy = $.isArray(object) ? [] : {};

            utils.eachPublic(object, function (key, value) {
                copy[key] = f(value);
            });

            return copy;
        },

        keys: function (object) {
            var result = [];

            $.each(object, function (key) {
                result.push(key);
            });

            return result;
        }
    };
}(window.jQuery));
/* jquery.signalR.knockout.diff.js */
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.knockout.utils.js" />

(function ($, undefined) {
    "use strict";

    var utils = $.signalR.knockout._utils;

    $.signalR.knockout.diffTools = function (builders) {
        var tagLookup = {};
        $.each(builders, function (_, builder) {
            tagLookup[builder.name] = builder;
        });

        this._builders = builders;
        this._tagLookup = tagLookup;
    };

    $.signalR.knockout.diffTools.prototype = {
        getBuilder: function (tag) {
            return this._tagLookup[tag] || {
                diffable: true,
                peek: utils.identity,
                create: utils.identity
            };
        },

        builderOf: function (value) {
            for (var i = 0; i < this._builders.length; i++) {
                if (this._builders[i].match(value)) {
                    return this._builders[i];
                }
            }
        },

        // Recursively tag important types so they can be rebuilt from JSON in other browsers
        // e.g. koObservable, koObservableArray
        tag: function (value) {
            var tag = $.proxy(this.tag, this),
                builder = this.builderOf(value);

            if (builder) {
                return {
                    _tag: builder.name,
                    value: utils.mapPublic(builder.peek(value), tag)
                };
            } else {
                return utils.mapPublic(value, tag);
            }
        },

        rebuild: function (taggedObject, parentTag) {
            var objectTag = utils.getTag(taggedObject),
                builder;

            // If parentTag is set, object is an escaped value
            if (parentTag !== "escape" && objectTag) {
                return this.rebuild(taggedObject.value, objectTag);
            }

            builder = this.getBuilder(parentTag);
            return builder.create(utils.mapPublic(taggedObject, $.proxy(this.rebuild, this)));
        },

        diff: function (before, after) {
            var getBuilder = $.proxy(this.getBuilder, this),
                finalDiff = {};

            // returns true if difference between "before" and "after"; false, otherwise.
            function diffImpl(before, after, result) {
                var modified = false,
                    visited = {},
                    builder = getBuilder(utils.getTag(after));

                if (!builder.diffable) {
                    $.extend(result, after);
                    result._replaced = true;
                    return true;
                }

                $.extend(result, before);

                // Keep diffs for each property that has changed
                // Delete properties that haven't changed
                $.each(after, function (key, value) {
                    var nestedDiff = {};

                    if (utils.isPrimitive(before[key]) || utils.isPrimitive(value)) {
                        // never drop tags
                        if (before[key] !== value || key === "_tag") {
                            result[key] = value;
                        } else {
                            delete result[key];
                        }
                    } else if (diffImpl(before[key], value, nestedDiff)) {
                        // recursive step found and return differences in nestedDiff
                        result[key] = nestedDiff;
                    } else {
                        // no differences at key
                        delete result[key];
                    }

                    visited[key] = true;
                });

                // Tag public properties in before but not after as deleted
                $.each(result, function (key, _) {
                    // result will be empty or contain only a _tag if there are no differences
                    if (key !== "_tag") {
                        modified = true;
                    }

                    if (!visited[key]) {
                        if (key !== "_tag") {
                            result[key] = { _tag: "delete" };
                        } else {
                            // Keys aren't merged into the model, so they don't need to be
                            // explicitly deleted. 
                            delete result[key];
                        }
                    }
                });

                return modified;
            }

            if (utils.isPrimitive(before) || utils.isPrimitive(after)) {
                return after;
            } else {
                diffImpl(before, after, finalDiff);
                return finalDiff;
            }
        },

        merge: function (model, diff, parentTag) {
            var merge = $.proxy(this.merge, this),
                diffTag = utils.getTag(diff),
                modelIsArray = $.isArray(model),
                builder,
                modelPeek,
                diffKeys;

            if (utils.isPrimitive(diff)) {
                model = diff;
            } else if (utils.isPrimitive(model)) {
                model = this.rebuild(diff);
            } else if (parentTag !== "escape" && diffTag) {
                // We need to use builder to create/update value since it is tagged
                builder = this.getBuilder(diffTag);

                if (diff._replaced) {
                    // Don't pass diffTag to rebuild since we update or create later
                    modelPeek = this.rebuild(diff.value);
                } else {
                    modelPeek = builder.peek(model);
                    modelPeek = merge(modelPeek, diff.value, diffTag);
                }

                if (builder.update) {
                    builder.update(model, modelPeek);
                } else {
                    $.extend(model, builder.create(modelPeek));
                    //model = builder.create(modelPeek);
                }
            } else {
                // Normal recursive case
                diffKeys = utils.keys(diff);

                if (modelIsArray) {
                    // Reverse iterate in case we are removing items from array
                    diffKeys = utils.mapPublic(diffKeys, parseInt).sort().reverse();
                }

                $.each(diffKeys, function (_, key) {
                    var nestedDiff = diff[key];
                    if (diffTag !== "escape" && utils.getTag(nestedDiff) === "delete") {
                        if (modelIsArray) {
                            model.splice(key, 1);
                        } else {
                            delete model[key];
                        }
                    } else {
                        model[key] = merge(model[key], nestedDiff);
                    }
                });
            }

            return model;
        }
    };
}(window.jQuery));
/* jquery.signalR.knockout.core.js */
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
