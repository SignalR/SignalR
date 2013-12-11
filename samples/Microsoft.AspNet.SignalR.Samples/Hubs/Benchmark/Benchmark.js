//hitMe / hitUs : sends the server the current timestamp and the number of calls to make back to the client.  hitMe: just the callign client, hitUs: all clients on the hub.
//stepOne / stepAll : increments a counter
//doneOne / doneAll : prints # of messages and total duration.

$(function () {
    function log(message) {
        var $newMessage = $("<li/>", { text: message });
        $("#messages").prepend($newMessage);
        return $newMessage;
    };

    var bench = $.connection.hubBench,
        countOne = 0,
        countAll = 0;

    bench.client.stepOne = function (ndx) {
        delete idSet[ndx + ""];
        ++countOne;
    };

    bench.client.doneOne = function (start, expected) {
        var duration = new Date().getTime() - start;
        var $msg = log(countOne + " in " + duration + "ms");
        var theCount = countOne;
        countOne = 0;
        if (expected != theCount) {
            $msg.css('color', 'red');
            // console.log(idSet);
        }
        else {
            $("#hitme").trigger('click');
        }
    };

    bench.client.stepAll = function (ndx) {
        delete idSet[ndx + ""];
        ++countAll;
    };

    bench.client.doneAll = function (start, expected, numConnections, connectionId) {
        var duration = new Date().getTime() - start;
        var $msg = log(countAll + " in " + duration + "ms.  " + numConnections + " connections");
        var theCount = countAll;
        countAll = 0;
        if (expected != theCount) {
            $msg.css('color', 'red');
            // console.log(idSet);
        }
        else {
            if (connectionId == $.connection.hub.id) {
                $("#hitus").trigger('click');
            }
        }
    };

    $.connection.hub.start(options, function () { log("connected"); });

    var idSet = {};
    function initSet(numCalls) {
        idSet = {};
        for (var i = 0; i < numCalls; i++) {
            idSet[i + ""] = i;
        }

    }
    //benchmark messages to just me
    $("#hitme").click(function () {
        var numCalls = parseInt($("#clientCalls").val());
        initSet(numCalls);
        var now = new Date().getTime();
        bench.server.hitMe(now, numCalls, $.connection.hub.id);
    });

    //benchmark messages to all clients
    $("#hitus").click(function () {
        var numCalls = parseInt($("#clientCalls").val());
        initSet(numCalls);
        var now = new Date().getTime();
        bench.server.hitUs(now, numCalls);
    });
});