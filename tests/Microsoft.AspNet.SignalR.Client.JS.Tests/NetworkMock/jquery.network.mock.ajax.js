// Ajax network mock
(function ($, window) {
    var savedAjax = $.ajax,
        network = $.network,
		ajaxData = {},
		ajaxIds = 0,
        ignoringMessages = false,
        fail = function (data, soft) {
            // We must close the request prior to calling error so that we can pass our own
            // reason for failing the request.
            ignoringMessages = true;
            data.Request.abort();
            ignoringMessages = false;

            if (!soft) {
                data.Settings.error(data, "error");
            }
        };

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
            if (ignoringMessages === false) {
                if (savedSuccess) {
                    savedSuccess.apply(this, arguments);
                }

                delete ajaxData[id];
            }
        }

        settings.error = function () {
            // We only want to execute methods if the request is not ignoringMessages, we sleep to swallow jquery related actions
            if (ignoringMessages === false) {
                if (savedError) {
                    savedError.apply(this, arguments);
                }

                delete ajaxData[id];
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
                fail(ajaxData[id], false);
            }, 0);
        }

        return request;
    };

    network.ajax = {
        disconnect: function (soft) {
            /// <summary>Disconnects the network so javascript transport methods are unable to communicate with a server.</summary>
            /// <param name="soft" type="Boolean">Whether the disconnect should be soft.  A soft disconnect indicates that transport methods are not notified of disconnect.</param>
            for (var key in ajaxData) {
                var data = ajaxData[key];

                fail(data, soft);
            }
        },
        connect: function () {
            /// <summary>Connects the network so javascript methods can continue utilizing the network.</summary>
            ignoringMessages = false;
        }
    };
})($, window);
