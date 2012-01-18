$(function () {
    // Pure client side hub
    var signalR = $.connection;
    signalR.mouseTracking.moveMouse = function (id, x, y) {
        if (id == this.id) {
            return;
        }

        updateCursor(id, x, y);
    };

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


    signalR.hub.start({ transport: activeTransport }, function () {
        signalR.mouseTracking.join(function () {

            $(document).mousemove(function (e) {
                signalR.mouseTracking.move(e.pageX, e.pageY);
                updateCursor(signalR.mouseTracking.id, e.pageX, e.pageY);
            });
        });
    });
});