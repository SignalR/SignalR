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
