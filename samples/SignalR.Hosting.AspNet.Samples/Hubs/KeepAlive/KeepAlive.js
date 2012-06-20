//hitMe / hitUs : sends the server the current timestamp and the number of calls to make back to the client.  hitMe: just the callign client, hitUs: all clients on the hub.
//stepOne / stepAll : increments a counter
//doneOne / doneAll : prints # of messages and total duration.

$(function () {
    function log(message) {
        var $newMessage = $("<li/>", { text: message });
        $("#messages").prepend($newMessage);
        return $newMessage;
    };
    var keepAliveHub = $.connection.keepAlive;
    $.connection.hub.start(function () { log("connected"); });
    $.connection.hub.keepAliveTimeout(function () {
        log("keepAliveTimeout");
    });
    //benchmark messages to just me
    $("#setKeepAlive").click(function () {
        var numCalls = parseInt($("#keepAlive").val());
        keepAliveHub.setKeepAlive(numCalls);
    });
});