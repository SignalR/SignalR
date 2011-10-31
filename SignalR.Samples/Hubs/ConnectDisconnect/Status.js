$(function () {
    var status = $.connection.status;

    status.joined = function (id, when) {
        if ($.connection.hub.clientId === id) {
            addMessage(id, 'blue');    
        }
        
        addMessage(id + ' joined at ' + when, 'green');
    };

    status.leave = function (id, when) {
        addMessage(id + ' left at ' + when, 'red');
    };

    function addMessage(value, color) {
        $('#messages').append('<li style="background-color:' + color + ';color:white">' + value + '</li>');
    }

    $.connection.hub.start(function () {
        status.join();
    });

});