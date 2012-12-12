(function ($, window) {
    if (document.commandLineTest) {
        /**************************************************************************************                                                          
        We need to disable SignalR's Cross domain detection for command line testing of the
        JS unit tests.  This is because the command line runner cannot load relative paths
        such as /signalr/hubs.  We need to provide it with http://localhost:1337/signalr/hubs.
        Granted, the command line runner still acts as if all requests are not cross domain.
        ***************************************************************************************/

        var originalIndexOf = String.prototype.indexOf,
            originalCreateElement = window.document.createElement;

        // Don't allow indexOf to detect the http text, this could collide with future tests but 
        // for the time being it prevents cross domain detection
        String.prototype.indexOf = function () {
            if (arguments[0] === "http") {
                return 0;
            }
            else {
                return originalIndexOf.apply(this, arguments);
            }
        }

        window.document.createElement = function () {
            var obj = originalCreateElement.apply(window.document, arguments);
            // Detect if we're creating an anchor tag, if so we need to re-map its protocol/host 
            // objects to trick SignalR lib
            if (arguments[0] === "a") {
                obj.protocol = window.location.protocol;
                obj.host = window.location.host;
            }

            return obj
        }
    }
})($, window);