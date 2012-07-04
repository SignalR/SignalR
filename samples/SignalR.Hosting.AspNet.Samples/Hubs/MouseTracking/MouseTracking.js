/// <reference path="../../Scripts/jquery-1.6.2.js" />
/// <reference path="../../Scripts/jquery.signalR.js" />

$(function () {
    // Pure client side hub
    var hubConnection = $.hubConnection(),
        hub = hubConnection.createProxy('MouseTracking');

    hub.on('moveMouse', function (id, x, y) {
        if (id == this.state.id) {
            return;
        }

        updateCursor(id, x, y);
    });

    function updateCursor(id, x, y) {
        var e = document.getElementById(id);
        if (!e) {
            e = $('<div id="' + id + '" class="icon icon-wand">' + id + '</div>').appendTo(document.body);
            e.css('position', 'absolute');
        }
        else {
            e = $(e);
        }
        e.css('left', x);
        e.css('top', y);
    }

    hubConnection.start({ transport: activeTransport })
        .done(function () {
            return hub.invoke('Join');
        })
        .done(function () {
            $(document).mousemove(function (e) {
                hub.invoke('Move', e.pageX, e.pageY);
                updateCursor(hub.state.id, e.pageX, e.pageY);
            });
        });
});