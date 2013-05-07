// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

/*global window:false */
/// <reference path="jquery.signalR.hubs.js" />

(function ($, window) {
    "use strict";

    var HubPipelineModule = function () {
        var that = this;

        that.beforeIncoming = function (context) {
            return true;
        };

        that.afterIncoming = function (context, error) { };

        that.incoming = function (next) {
            return function (context) {
                if (that.beforeIncoming()) {
                    var error;

                    try {
                        next(context);
                    }
                    catch (ex) {
                        error = ex;
                    }
                    finally {
                        that.afterIncoming(context, error);
                    }
                }
            };
        };
    },
    HubPipeline = function (proxy) {
        var that = this,
            modules = [],
            compose = function (moduleProperty, endpoint) {
                var composition = function () {
                        endpoint.apply(proxy, arguments);
                    };

                if (modules.length > 0) {

                    for (var i = 1; i < modules.length; i++) {
                        composition = modules[i][moduleProperty](composition);
                    }
                }

                that._.invoker[moduleProperty] = composition;
            };

        that._ = {
            invoker: {
                incoming: function (action) { }
            },
            composeIncoming: function (endpoint) {
                compose("incoming", endpoint);
            }
        };

        that.incoming = function (incomingModule) {
            var module = new HubPipelineModule();

            // Fill in defaults for any non-set properties                
            modules.beforeIncoming = incomingModule.before || modules.beforeIncoming;
            modules.afterIncoming = incomingModule.after || modules.afterIncoming;

            modules.push(module);
        };
    };

    $.hubConnection._.HubPipeline = HubPipeline;

}(window.jQuery, window));