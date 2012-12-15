(function ($, window) {    
    // These values are updated via the csproj based on configuration values passed into its build
    window.document.testUrl = /*URL*/'auto'/*URL*/;
    window.document.commandLineTest = /*CMDLineTest*/false/*CMDLineTest*/;

    window.createHubConnection = function () {
        if (window.document.testUrl !== 'auto') {
            return $.hubConnection(window.document.testUrl);
        }
        return $.hubConnection();
    };
})($, window);