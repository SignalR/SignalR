// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

(function ($, window) {
    function getParameterByName(name, url) {
        if (!url) {
            url = window.location.href;
        }
        name = name.replace(/[\[\]]/g, "\\$&");
        var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)", "i"),
            results = regex.exec(url);
        if (!results) return null;
        if (!results[2]) return '';
        return decodeURIComponent(results[2].replace(/\+/g, " "));
    }

    // Disable JSONP tests. When they are active, we have to install a global onerror to suppress global failures
    // because JSONP aborts trigger global failures :(
    window.document.jsonpTestsEnabled = false;
    if (getParameterByName("jsonpEnabled") === "true") {
        window.document.jsonpTestsEnabled = true;
    }

    // Try to get the testUrl from the query string
    window.document.testUrl = getParameterByName("testUrl");

    // If we're being run in Karma, try the karma args
    if (!window.document.testUrl && window.__karma__) {
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

    // Normalize the URL, if present
    if (window.document.testUrl && !window.document.testUrl.endsWith("/")) {
        window.document.testUrl += "/";
    }

    var signalRUrl = "signalr/js";
    if (!window.document.testUrl) {
        window.document.testUrl = 'auto';
    } else {
        signalRUrl = window.document.testUrl + "signalr/js";
    }

    var scriptTag = document.createElement("script");
    scriptTag.src = signalRUrl;
    scriptTag.crossOrigin = "anonymous";
    document.body.appendChild(scriptTag);

    var runUnit = getParameterByName("runUnit") !== "false";
    var runFunctional = getParameterByName("runFunctional") !== "false";
    window._config = {
        runUnit: runUnit,
        runFunctional: runFunctional
    };

    // Disable auto-start. We need to wait until signalr/js has loaded before we can run the tests
    QUnit.config.autostart = false;
    if (getParameterByName("autostart") !== "false") {
        window.addEventListener("load", function () {
            // If the hub proxies have loaded, 'proxies' will be set and we're ready to go
            if ($.connection.hub.proxies) {
                console.log("proxies are loaded, starting tests!");
                QUnit.start();
            }
            else {
                console.log("waiting for proxies to load...");
                // Start an interval timer
                var intervalHandle;
                intervalHandle = setInterval(function () {
                    if ($.connection.hub.proxies) {
                        console.log("proxies are loaded, starting tests!");
                        QUnit.start();
                        clearInterval(intervalHandle);
                    }
                    else {
                        console.log("waiting for proxies to load...");
                    }
                }, 500);
            }
        });
    }

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

    // In Karma, make sure the test name is in the log message so we can split things up.
    if (window.__karma__) {
        ["debug", "log", "error", "warn", "info"].forEach(function (method) {
            var savedVersion = console[method];
            console[method] = function () {
                var args = Array.prototype.slice.call(arguments);
                if (QUnit.config.current) {
                    args[0] =
                        QUnit.config.current.module.name +
                        "/" +
                        QUnit.config.current.testName +
                        "||" +
                        args[0]
                }
                savedVersion.apply(console, args);
            }
        });
    }

    QUnit.testStart(function (details) {
        console.log("***** STARTING TEST [" + details.module + "/" + details.name + "] *****")
    });
    QUnit.testDone(function (details) {
        console.log("***** FINISHED TEST [" + details.module + "/" + details.name + "] *****")
    });
})($, window);
