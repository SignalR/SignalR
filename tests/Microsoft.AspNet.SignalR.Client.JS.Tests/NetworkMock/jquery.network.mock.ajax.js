// Ajax network mock
(function ($, window) {
    var savedAjax = $.ajax,
        modifiedAjax,
        network = $.network,
        ajaxData = {},
        ajaxIds = 0,
        ignoringMessages = false,
        failSoftly = false,
        disableAbortError = false,
        fail = function (id) {
            var data = ajaxData[id];
            if (data) {
                // We must close the request prior to calling error so that we can pass our own
                // reason for failing the request.
                disableAbortError = true;
                data.Request.abort();
                disableAbortError = false;

                data.Settings.error({ responseText: "NetworkMock: Disconnected." });

                delete ajaxData[id];
            }
        };

    function failAllOngoingRequests() {
        $.each(ajaxData, function (id, _) {
            fail(id);
        });
    }

    $.ajax = function (url, settings) {
        var request,
            savedSuccess,
            savedError,
            id = ajaxIds++;

        if (!settings) {
            settings = url;
        }

        savedSuccess = settings.success;
        savedError = settings.error;

        settings.success = function () {
            // We only want to execute methods if the request is not ignoringMessages, we sleep to swallow jquery related actions
            if (!ignoringMessages && ajaxData[id]) {
                delete ajaxData[id];
                if (savedSuccess) {
                    savedSuccess.apply(this, arguments);
                }
            }
        }

        settings.error = function () {
            // We only want to execute methods if the request is not ignoringMessages, we sleep to swallow jquery related actions
            if (ajaxData[id] && !disableAbortError) {
                delete ajaxData[id];
                if (!failSoftly) {
                    if (savedError) {
                        savedError.apply(this, arguments);
                    }
                }
            }
        }

        request = savedAjax.apply(this, [url, settings]);
        ajaxData[id] = {
            Request: request,
            Settings: settings
        };

        // If we're trying to make an ajax request while the network is down
        if (ignoringMessages) {
            // Act async for failure of request
            setTimeout(function () {
                fail(id);
            }, 0);
        }

        return request;
    };

    modifiedAjax = $.ajax;

    network.ajax = {
        enable: function () {
            /// <summary>Enables the Ajax network mock functionality.</summary>
            $.ajax = modifiedAjax;
        },
        disable: function () {
            /// <summary>Disables the Ajax network mock functionality.</summary>
            $.ajax = savedAjax;
        },
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>

            // Ensure we don't set ignoringMessages to true after calling failAllOngoingRequests,
            // because we might call connect in connection.reconnecting which can run synchronously.
            ignoringMessages = true;
            failSoftly = soft === true;
            failAllOngoingRequests();
            failSoftly = false;
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>

            // fail all ongoing requests synchronously so we don't have to worry about any requests failing unnecessarily after connect completes.
            failAllOngoingRequests();
            ignoringMessages = false;
        }
    };
})($, window);
