// Masks network mock
(function ($, window) {
    var network = $.network,
        maskData = {},
        maskIds = 0,
        ignoringMessages = false;

    network.mask = {
        create: function (maskBase, onErrorProperty, onMessageProperty, destroyOnError) {
            /// <summary>Wires the maskBase's onError and onMessage properties to be affected by network loss</summary>
            /// <param name="maskBase" type="Object">An object with properties indicated by onErrorProperty and onMessageProperty</param>
            /// <param name="onErrorProperty" type="String">The string representation of the onError method of the maskBase</param>
            /// <param name="onMessageProperty" type="String">The string representation of the onMessage method of the maskBase</param>
            /// <param name="destroyOnError" type="Boolean">Represents if we destroy our created mask object onError.</param>
            var savedOnError = maskBase[onErrorProperty],
                savedOnMessage = maskBase[onMessageProperty],
                id = maskIds++;

            if (!savedOnError || !savedOnMessage) {
                throw new Error("maskBase must have an onErrorProperty AND an onMessageProperty property");
            }

            maskBase[onErrorProperty] = function () {
                if (!ignoringMessages) {
                    return savedOnError.apply(this, arguments);
                }

                if (destroyOnError) {
                    // Reset the maskBase to its original setup
                    maskBase[onErrorProperty] = savedOnError;
                    maskBase[onMessageProperty] = savedOnMessage;
                    delete maskData[id];
                }
            };

            maskBase[onMessageProperty] = function () {
                if (!ignoringMessages) {
                    return savedOnMessage.apply(this, arguments);
                }
            };

            maskData[id] =
            {
                onError: maskBase[onErrorProperty],
                onMessage: maskBase[onMessageProperty]
            };
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
            if (!soft) {
                for (var key in maskData) {
                    var data = maskData[key];

                    data.onError();
                }
            }
            else {
                ignoringMessages = true;
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
