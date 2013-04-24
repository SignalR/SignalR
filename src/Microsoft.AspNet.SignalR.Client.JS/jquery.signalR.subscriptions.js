(function ($) {
    //    var fn = $.signalR.subscribe("hub", "method", function(data){ /* do something */ });
    //    $.signalR.unsubscribe(fn);
    //    $.signalR.onConnected(function(clientId){ /* do something */ });
    
    var signalr_conn_timeout, onConnect, onDisconnect, handleInt,
        validateSub, subscriptions, connected, log, init, notifyConnected,
        connectedSubs;

    // identity seed for handle names
    handleInt = 0;

    // is signalR connected
    connected = false;
    connectedSubs = [];

    // tracks all subscriptions and subscription handlers
    subscriptions = { _list: [] };

    // performs connection to hubs when connected
    onConnect = function () {
        var hubs, current;
        // loop through and create a receiver for any that require it.

        hubs = $.connection;
        for (var i = 0; i < subscriptions._list.length; ++i) {
            current = subscriptions[subscriptions._list[i]];
            if (hubs.hasOwnProperty(current._hub) && !hubs[current._hub].hasOwnProperty(current._method)) {
                if (connected) {
                    throw "Already connected, please restructure to create subscription at beginning of page load";
                }
                hubs[current._hub].client[current._method] = current._handle;
                if (init) clearTimeout(init);
                init = setTimeout(function () {
                    $.connection.hub.start()
                        .done(function (mhc) { log('SignalR connected'); connected = true; notifyConnected(); })
                        .fail(function (mhc) { log('SignalR connection failed'); });
                }, 1000);
            }
        }
    }

    notifyConnected = function () {
        var i, sub;
        for (i = 0; i < connectedSubs.length; ++i)
        {
            connectedSubs[i]($.connection.hub.id);
        }
        for (i = 0; i < subscriptions._list.length; ++i)
        {
            sub = subscriptions[subscriptions._list[i]];
            if (sub.hasOwnProperty("onConnected"))
            {
                sub.onConnected($.connection.hub.id)
            }
        }
    }

    // removes connection to specific hub
    onDisconnect = function (hub, method) {
        // loop through and remove receivers.
        if (!connected) return;
        delete $.connection[hub].client[method];
    }

    // utility logger
    log = function (message) {
        if (window.console && window.console.log)
            window.console.log(message);
    }

    // validate that subscription is well formed
    validateSub = function (settings) {
        var errorstring = "";
        if (typeof settings.hub != "string") {
            errorstring += "expected a hub name";
        }
        if (typeof settings.method != "string") {
            errorstring += "expected a method name";
        }
        if (typeof settings.handle != "string") {
            errorstring += "expected a handle name";
        }
        if (typeof settings.fn != "function") {
            errorstring += "expected a function to handle";
        }
        return errorstring
    }

    $.extend(true, $, {
        signalR: {
            subscribe: function () {
                /// <signature>
                /// <summary>
                /// subscribe to a signalR hub
                /// </summary>
                /// <param name="setting" type="PlainObject">A set of key/value pairs that configure the hub subscription. minimum requires hub, method and function to be bound.</param>
                /// <returns type="function">function handler passed in that is now registered</returns>
                /// </signature>
                /// <signature>
                /// <summary>
                /// subscribe to a signalR hub
                /// </summary>
                /// <param name="hub" type="string">name of the hub</param>
                /// <param name="method" type="string">method of the hub</param>
                /// <param name="fn" type="function">function handler</param>
                /// <returns type="function">function handler passed in that is now registered</returns>
                /// </signature>
                /// <signature>
                /// <summary>
                /// subscribe to a signalR hub
                /// </summary>
                /// <param name="hub" type="string">name of the hub</param>
                /// <param name="method" type="string">method of the hub</param>
                /// <param name="settings" type="PlainObject">A set of key/value pairs that configure the hub subscription. minimum requires the function to be bound.</param>
                /// <returns type="function">function handler passed in that is now registered</returns>
                /// </signature>
                /// <signature>
                /// <summary>
                /// subscribe to a signalR hub
                /// </summary>
                /// <param name="hub" type="string">name of the hub</param>
                /// <param name="method" type="string">method of the hub</param>
                /// <param name="handle" type="string">handle to use for the subscription</param>
                /// <param name="fn" type="function">function handler</param>
                /// <returns type="function">function handler passed in that is now registered</returns>
                /// </signature>

                var fnObj, handle, sub, fn;
                fnObj = {
                    hub: null,
                    method: null,
                    handle: null,
                    fn: null
                };

                // hydrate our settings object
                if (arguments.length == 1 && typeof arguments[0] == "object") {
                    fnObj = $.extend(true, fnObj, arguments[0]);
                } else if (arguments.length == 3 && typeof arguments[0] == "string"
                    && typeof arguments[1] == "string" &&
                    (typeof arguments[2] == "function" || typeof arguments[2] == "object")) {
                    fnObj.hub = arguments[0];
                    fnObj.method = arguments[1];
                    if (typeof arguments[2] == "function") {
                        fnObj.fn = arguments[2];
                    }
                    else {
                        $.extend(true, fnObj, arguments[2]);
                    }
                } else if (arguments.length == 3 && typeof arguments[0] == "string"
                    && typeof arguments[1] == "string" && typeof arguments[2] == "string"
                    && typeof arguments[3] == "function") {

                    fnObj.hub = arguments[0];
                    fnObj.method = arguments[1];
                    fnObj.handle = arguments[2];
                    fnObj.fn = arguments[3];
                }
                if (!fnObj.handle) {
                    handle = "signalR_" + handleInt;
                    ++handleInt;
                    fnObj.handle = handle;
                }

                // validate settings object
                var valid = validateSub(fnObj);
                if (valid.length > 0) throw valid;

                // create handle keys
                fnObj.internalhandle = fnObj.hub + "_" + fnObj.method + "_" + fnObj.handle;
                fnObj.internalsub = fnObj.hub + "_" + fnObj.method;
                fnObj.fn.handle = fnObj.internalhandle;

                // create endpoint handler
                if (!subscriptions.hasOwnProperty(fnObj.internalsub)) {
                    subscriptions[fnObj.internalsub] = new (function () {
                        var me = this;
                        me._hub = fnObj.hub;
                        me._method = fnObj.method;
                        me._handle = function (data) {
                            var i, h;
                            log("Received data for " + me._hub + "." + me._method);
                            for (i = 0; i < me.subscriptions.length; ++i) {
                                h = me[me.subscriptions[i]].fn;
                                log("sending " + me._hub + "." + me._method + " data to " + me.subscriptions[i]);
                                h(data);
                            }
                        };
                        me.subscriptions = new Array();
                        log("registered handler for " + me._hub + "." + me._method);
                    })();

                    // register endpoint handle key
                    subscriptions._list.push(fnObj.internalsub);
                    onConnect();
                }
                // retrieve endpoint handler 
                sub = subscriptions[fnObj.internalsub];

                // check if there is already a subscriber registered
                if (sub.hasOwnProperty(fnObj.internalhandle)) {
                    fn = sub[fnObj.internalhandle];
                    if (fn.hasOwnProperty("onDisconnect") && typeof fn.onDisconnect == "function") {
                        fn.onDisconnect("Another method has usurped this subscription");
                    }
                }
                else {
                    // register subscription handler key
                    sub.subscriptions.push(fnObj.internalhandle);
                }

                // store subscriptions
                sub[fnObj.internalhandle] = fnObj;
                subscriptions[fnObj.internalhandle] = fnObj;

                log("subscription for " + fnObj.handle + " to " + fnObj.hub + "." + fnObj.method + " registered")

                // return registered function for later use by subscriber owner
                return fnObj.fn;
            },
            unsbuscribe: function (handle) {
                /// <signature>
                /// <summary>
                /// unsubscribe from a signalR hub
                /// </summary>
                /// <param name="handle" type="string">the handler key attached to the function handler <i>[function].handler</i></param>
                /// </signature>
                /// <signature>
                /// <summary>
                /// unsubscribe from a signalR hub
                /// </summary>
                /// <param name="handle" type="function">the function handler</param>
                /// </signature>
                var sub, hb, is, h, m, hnd;

                // retrieve function handler key
                hnd = typeof handle == "function" ? handle.handle : handle;

                // if not found exit
                if (!subscriptions.hasOwnProperty(hnd)) return;

                // get references
                sub = subscriptions[hnd];  // subscription handler
                hb = subscriptions[sub.internalsub]; // endpoint handler

                // get identifiers
                is = sub.internalsub;
                h = sub.hub;
                m = sub.method;

                // notify owning object
                if (sub.hasOwnProperty("onDisconnect"))
                    sub.onDisconnect("Unsubscribed from " + sub.hub + "." + sub.method);

                // delete references
                delete subscriptions[hnd];
                delete hb[hnd];
                hb.subscriptions = hb.subscriptions.splice(hb.subscriptions.indexOf(hnd), 1);

                // disconnect if its no longer necessary
                if (Object.keys(hb).length == 0) {
                    delete subscriptions[is];
                    subscriptions._list = subscriptions._list.splice(subscriptions._list.indexOf(is), 1);
                    onDisconnect(h, m);
                }
            },
            onConnected: function (fn) {
                /// <signature>
                /// <summary>
                /// function to be called when signalr is connected
                /// </summary>
                /// <param name="fn" type="function(conenctionid)">function to be called</param>
                /// </signature>
                if (typeof fn != "function") throw "Expected a function";
                if (connected) {
                    fn($.connection.hub.id);
                }
                else {
                    connectedSubs.push(fn);
                }
            }
        }
    });
    $.connection.hub.logging = true;

    $.connection.hub.error(function (error) {
        log('SignalR error' + error.toString());
    });

    $.connection.hub.stateChanged(function (change) {
        if (change.newState === $.signalR.connectionState.reconnecting) {
            clearTimeout(signalr_conn_timeout);
            signalr_conn_timeout = setTimeout(function () {
                log('SignalR trying to reconnect');
            }, 10000);
        } else if (signalr_conn_timeout && change.newState === $.signalR.connectionState.connected) {
            log('SignalR is connected');
            clearTimeout(signalr_conn_timeout);
            signalr_conn_timeout = null;
        } else if (change.newState === $.signalR.connectionState.disconnected) {
            log('SignalR is disconnected');
            clearTimeout(signalr_conn_timeout);
            signalr_conn_timeout = setTimeout(function () {
                log('SignalR restart due to disconnect');
                $.connection.hub.start();
            }, 60000);
        }
    });
})(window.jQuery);
