var testUtilities;

(function ($, window) {
    var ios = !!((navigator.userAgent.match(/iPhone/i)) || (navigator.userAgent.match(/iPod/i))),
        ios6 = !!(ios && navigator.userAgent.indexOf("CPU OS 6_0") >= 0);

    testUtilities = {
        createHubConnection: function () {
            var connection;

            if (window.document.testUrl !== 'auto') {
                connection = $.hubConnection(window.document.testUrl);
            } else {
                connection = $.hubConnection('signalr', { useDefaultPath: false });
            }

            connection.logging = true;

            return connection;
        },
        createConnection: function (url) {
            var connection;

            if (window.document.testUrl !== 'auto') {
                connection = $.connection(window.document.testUrl + '/' + url);
            } else {
                connection = $.connection(url);
            }

            connection.logging = true;

            return connection;
        },
        webSocketsEnabled: (function () {
            var validPlatform = true;

            if (ios && !ios6) {
                validPlatform = false;
            }

            return !!(window.WebSocket && !window.document.commandLineTest && validPlatform);
        })(),
        foreverFrameEnabled: (function () {
            return !window.EventSource && !window.document.commandLineTest && navigator.appName === "Microsoft Internet Explorer";
        })(),
        serverSentEventsEnabled: (function () {
            return !!window.EventSource;
        })(),
        longPollingEnabled: (function () {
            return true;
        })()
    };
})($, window);