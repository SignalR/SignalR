// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

(function ($, window) {
    function getParameterByName(name, url) {
        if (!url) {
            url = window.location.href;
        }
        name = name.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
            results = regex.exec(url);
        if (!results) return null;
        if (!results[2]) return '';
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }

    // Disable JSONP tests. When they are active, we have to install a global onerror to suppress global failures
    // because JSONP aborts trigger global failures :(
    window.document.jsonpTestsEnabled = false;
    if(getParameterByName("jsonpEnabled") === "true") {
        window.document.jsonpTestsEnabled = true;
    }

    // If we're being run in Karma
    if (window.__karma__) {
        // Try to set the testUrl based on the args
        const args = window.__karma__.config.args;

        for (let i = 0; i < args.length; i += 1) {
            switch (args[i]) {
                case "--server":
                    i += 1;
                    window.document.testUrl = args[i];
                    break;
            }
        }
    }
    else {
        // Check the URL
        window.document.testUrl = getParameterByName("testUrl");
    }

    if (window.document.testUrl && !window.document.testUrl.endsWith("/")) {
        window.document.testUrl += "/";
    }

    var signalRUrl = "signalr/js";
    if (!window.document.testUrl) {
        window.document.testUrl = 'auto';
    } else {
        signalRUrl = window.document.testUrl + "signalr/js";
    }
    document.write("<script src=\"" + signalRUrl + "\" crossorigin=\"anonymous\"></script>");

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
                    console.log("Network Mock Error occurred, unable to stop iframe.  Message = " + e.message);
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