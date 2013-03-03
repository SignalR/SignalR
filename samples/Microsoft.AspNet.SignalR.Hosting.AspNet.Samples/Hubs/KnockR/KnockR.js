/// <reference path="../../Scripts/jquery-1.6.4.js" />
/// <reference path="../../Scripts/knockout-2.2.1.debug.js" />
/// <reference path="../../Scripts/jquery.signalR.js" />


(function () {
    $.connection.listHub.observable();

    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: activeTransport, jsonp: isJsonp });
})();