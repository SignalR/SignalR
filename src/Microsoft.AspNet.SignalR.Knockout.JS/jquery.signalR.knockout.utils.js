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
            // TODO: We shouldn't have to make this check. Look into why we do.
            if (window.ko.isWriteableObservable(x)) {
                return x(newValue);
            }
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
