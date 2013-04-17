(function ($, window) {
    if (document.commandLineTest) {
        /**************************************************************************************                                                          
        We need to disable SignalR's Cross domain detection for command line testing of the
        JS unit tests.  This is because the command line runner cannot load relative paths
        such as /signalr/js.  We need to provide it with http://localhost:1337/signalr/js.
        Granted, the command line runner still acts as if all requests are not cross domain.
        ***************************************************************************************/

        // Maintain a reference to the original isCrossDomain function
        var signalRCrossDomain = $.connection.prototype.isCrossDomain;

        window.document.crossDomainDisabled = true;        

        $.connection.prototype.isCrossDomain = function () {
            if (window.document.crossDomainDisabled) {
                return false;
            }
            else {
                return signalRCrossDomain.apply(this, arguments);
            }
        }
    }
})($, window);