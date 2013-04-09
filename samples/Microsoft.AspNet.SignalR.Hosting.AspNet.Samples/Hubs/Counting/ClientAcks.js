$(function () {
    var hub = $.connection.countingHub,
        prev,
        running = false;

    hub.client.send = function (n) {
        var expected = null;

        if (prev) {
            expected = prev + 1;
        }
        
        if (expected === null || expected == n) {
            $('#value').html('<span>' + n + '</span>');
            prev = n;

            if (running) {
                hub.server.send(n + 1);
            }
        }
        else {
            $('#value').html('<span style="color:red"> Expected: ' + expected + '</span>');
        }
    };

    $.connection.hub.logging = true;
    $.connection.hub.start().done(function () {
        $('#go').click(function () {
            if (running === false) {
                hub.server.send(0);
                running = true;
            }
        });
    });
});