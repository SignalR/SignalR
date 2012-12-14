(function ($, window) {
    if (document.commandLineTest) {
        /**************************************************************************************                                                          
        We need to disable SignalR's Cross domain detection for command line testing of the
        JS unit tests.  This is because the command line runner cannot load relative paths
        such as /signalr/hubs.  We need to provide it with http://localhost:1337/signalr/hubs.
        Granted, the command line runner still acts as if all requests are not cross domain.
        ***************************************************************************************/

        $.connection.prototype.isCrossDomain = function () {
            return false;
        }
    }
})($, window);