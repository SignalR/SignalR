(function ($, window) {    
    // These values are updated via the csproj based on configuration values passed into its build
    window.document.testUrl = /*URL*/'auto'/*URL*/;
    window.document.commandLineTest = /*CMDLineTest*/false/*CMDLineTest*/;

    function failConnection() {
        $("iframe").each(function () {
            var loadEvent;

            if (this.stop) {
                this.stop();
            } else {
                try {
                    cw = this.contentWindow || this.contentDocument;
                    if (cw.document && cw.document.execCommand) {
                        cw.document.execCommand("Stop");
                    }
                }
                catch (e) {
                    console.log("Network Mock Error occured, unable to stop iframe.  Message = " + e.message);
                }
            }

            if (window.document.createEvent) {
                loadEvent = window.document.createEvent("Event");
                loadEvent.initEvent("load", true, true);
            } else {
                loadEvent = window.document.createEventObject();
                loadEvent.eventType = "load";
            }

            if (this.dispatchEvent) {
                this.dispatchEvent(loadEvent);
            } else {
                this.fireEvent("onload", loadEvent);
            }
        });
    }

    // Configure masks to help mock foreverFrame transports and ajaxSends to be used for network mocking
    $.signalR.transports.foreverFrame.networkLoss = failConnection;
    $.network.mask.create($.signalR.transports.foreverFrame, ["networkLoss"], ["receive"]);
    $.network.mask.subscribe($.signalR.transports.foreverFrame, "started", failConnection);

})($, window);