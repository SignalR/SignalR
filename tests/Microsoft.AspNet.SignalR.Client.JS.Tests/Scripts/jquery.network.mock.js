/* jquery.network.mock.core.js */
// Network core
var network = {};

(function ($, window, network) {
})($, window, network);
/* jquery.network.mock.ajax.js */
// Ajax network mock
(function ($, window, network) {
    var savedAjax = $.ajax,
		ajaxData = {},
		ajaxIds = 0,
        sleeping = false,
        // Used to not trigger any methods from a resultant ajax completion event.
        silentCall = function (fn) {
            sleeping = true;
            fn();
            sleeping = false;
        },
		disconnect = function (soft) {
		    for (var key in ajaxData) {
		        var data = ajaxData[key];

		        silentCall(function () { data.Request.abort(); });

		        if (!soft) {
		            data.Settings.error(ajaxData[key], "error");
		        }
		    }
		};

    $.ajax = function (url, settings) {
        var request,
			savedSuccess,
			savedError,
			id = ajaxIds;

        if (!settings) {
            settings = url;
        }

        savedSuccess = settings.success;
        savedError = settings.error;

        settings.success = function () {
            // We only want to execute methods if the request is not sleeping, we sleep to swallow jquery related actions
            if (sleeping === false) {
                if (savedSuccess) {
                    savedSuccess.apply(this, arguments);
                }

                delete ajaxData[id];
            }
        }

        settings.error = function () {
            // We only want to execute methods if the request is not sleeping, we sleep to swallow jquery related actions
            if (sleeping === false) {
                if (savedError) {
                    savedError.apply(this, arguments);
                }

                delete ajaxData[id];
            }
        }

        request = savedAjax.apply(this, [url, settings]);
        ajaxData[ajaxIds++] = {
            Request: request,
            Settings: settings
        };

        return request;
    };

    network.ajax = {
        disconnect: function (soft) {
            disconnect(soft);
        }
    };
})($, window, network);
/* jquery.network.mock.websocket.js */
// Web Socket network mock
(function ($, window, network) {
})($, window, network);
/* jquery.network.mock.eventsource.js */
// Event Source network mock
(function ($, window, network) {
})($, window, network);
/* jquery.network.mock.iframe.js */
// iFrame network mock
(function ($, window, network) {
})($, window, network);
