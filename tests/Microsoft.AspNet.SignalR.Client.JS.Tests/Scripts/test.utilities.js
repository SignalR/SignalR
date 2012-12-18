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
    }
};