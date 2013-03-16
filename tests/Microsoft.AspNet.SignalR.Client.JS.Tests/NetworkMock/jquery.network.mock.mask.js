// Masks network mock
(function ($, window) {
    var network = $.network,
        maskData = {},
        maskIds = 0,
        ignoringMessages = false;

    network.mask = {
        create: function (maskBase, onErrorProperties, subscriptionProperties, destroyOnError) {
            /// <summary>Wires the maskBase's onError and onMessage properties to be affected by network loss</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by onErrorProperty and onMessageProperty</param>
            /// <param name="onErrorProperties" type="Array">The array of string representation of the onError methods of the maskBase</param>
            /// <param name="subscriptionProperties" type="Array">The array of string representation of the methods subscribed to network events on the maskBase</param>
            /// <param name="destroyOnError" type="Boolean">Represents if we destroy our created mask object onError.</param>
            var savedOnErrors = {},
                savedSubscriptions = {},
                id = maskIds++;

            function resetMaskBase() {
                $.extend(maskBase, savedOnErrors);
                $.extend(maskBase, savedSubscriptions);
                delete maskData[id];
            }

            maskData[id] = {
                onError: [],
                onMessage: []
            };

            $.each(onErrorProperties, function (_, onErrorProperty) {
                var saved = maskBase[onErrorProperty];

                if (!saved) {
                    throw new Error("maskBase must have each specified onError property.");
                }

                maskBase[onErrorProperty] = function () {
                    if (destroyOnError) {
                        resetMaskBase();
                    }

                    return saved.apply(this, arguments);
                }

                maskData[id].onError.push(maskBase[onErrorProperty]);
                savedOnErrors[onErrorProperty] = saved;
            });

            
            $.each(subscriptionProperties, function (_, subscriptionProperty) {
                var saved = maskBase[subscriptionProperty];

                if (!saved) {
                    throw new Error("maskBase must have each specified subscription property.");
                }

                maskBase[subscriptionProperty] = function () {
                    if (!ignoringMessages) {
                        return saved.apply(this, arguments);
                    }
                };

                maskData[id].onMessage.push(maskBase[subscriptionProperty]);
                savedSubscriptions[subscriptionProperty] = saved;
            });
        },
        subscribe: function (maskBase, functionName, onBadAttempt) {
            /// <summary>Subscribes a function to only be called when the network is up</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by functionName</param>
            /// <param name="functionName" type="String">Function name that should be replaced on the maskBase</param>
            /// <param name="onBadAttempt" type="Function">Function to only be called when an attempt to call the functionName function is called when network is down</param>
            /// <returns type="Object">Object with an unsubscribe method used to remove the network connection monitoring.</returns>
            var savedFunction = maskBase[functionName];

            maskBase[functionName] = function () {
                if (!ignoringMessages) {
                    return savedFunction.apply(this, arguments);
                }
                else if (onBadAttempt) {
                    onBadAttempt();
                }
            };

            return {
                unsubscribe: function () {
                    maskBase[functionName] = savedFunction;
                }
            };
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>

            // Ensure we don't set ignoringMessages to true after calling errorFunc, because we
            // might call connect in connection.reconnecting which can run synchronously.
            ignoringMessages = true;
            if (!soft) {
                $.each(maskData, function (_, data) {
                    $.each(data.onError, function (_, errorFunc) {
                        errorFunc();
                    });
                });
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
