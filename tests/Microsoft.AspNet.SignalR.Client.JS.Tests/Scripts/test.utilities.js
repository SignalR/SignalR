var testUtilities;

(function ($, window) {
    testUtilities = {
        createHubConnection: function () {
            var connection;

            if (window.document.testUrl !== 'auto') {
                connection = $.hubConnection(window.document.testUrl, { useDefaultPath: false });
            }
            else {
                connection = $.hubConnection('signalr', { useDefaultPath: false });
            }

            connection.logging = true;

            return connection;
        },
        webSocketsEnabled: (function() {
            return !!((window.MozWebSocket || window.WebSocket) && !window.document.commandLineTest);
        })(),
        foreverFrameEnabled: (function () {
            return !window.EventSource && !window.document.commandLineTest;
        })(),
        serverSentEventsEnabled: (function () {
            return !!window.EventSource;
        })(),
        longPollingEnabled: (function () {
            return true;
        })()
    };
})($, window);