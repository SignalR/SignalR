/// <reference path="../../Scripts/jquery-1.6.2.js" />
/// <reference path="../../Scripts/jquery.signalR.js" />

$(function () {
    var hubConnection = $.hubConnection('/signalr', { qs: 'test=1', logging: false, useDefaultPath: false }),
        hub = hubConnection.createProxy('mouseTracking');


    hub.on('MoveMouse', function (id, x, y) {
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
        e.css({ left: x, top: y });
    }

    hubConnection.logging = true;
    hubConnection.start({ transport: activeTransport })
        .pipe(function () {
            return hub.invoke('join');
        })
        .pipe(function () {
            $(document).mousemove(function (e) {
                hub.invoke('Move', e.pageX, e.pageY);
                updateCursor(hub.state.id, e.pageX, e.pageY);
            });
        });
});