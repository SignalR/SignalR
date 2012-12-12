(function ($, window) {    
    // These values are updated via the csproj based on configuration values passed into its build
    window.document.testUrl = /*URL*/'auto'/*URL*/;
    window.document.commandLineTest = /*CMDLineTest*/false/*CMDLineTest*/;

    if (window.document.testUrl !== 'auto') {
        $.connection.hub.url = window.document.testUrl;
    }
})($, window);