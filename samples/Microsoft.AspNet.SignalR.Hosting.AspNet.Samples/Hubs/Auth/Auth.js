$(function () {
    var hubs = {
        'Messages': $.connection.noAuthHub,
        'Messages Invoked By Admin or Invoker': $.connection.invokeAuthHub,
        'Messages Requiring Authentication to Send or Receive': $.connection.authHub,
        'Messages Requiring Authentication to Send or Receive Because of Inheritance': $.connection.inheritAuthHub,
        'Messages Requiring Authentication to Send': $.connection.incomingAuthHub,
        'Messages Requiring Admin Membership to Send or Receive': $.connection.adminAuthHub,
        'Messages Requiring Name to be "User" and Role to be "Admin" to Send or Receive': $.connection.userAndRoleAuthHub
    };

    $.each(hubs, function (title, hub) {
        var list = $('<ul>').appendTo($('#messages').append('<h4>' + title + '</h4>')),
            addMessage = function (value, className) {
                list.append('<li class="' + className + '">' + value + '</li>');
            };

        $.extend(hub.client, {
            joined: function (id, when, authInfo) {
                var info = [];
                if ($.connection.hub.id === id) {
                    addMessage(id, 'blue');
                }
                $.each(authInfo, function (k, v) {
                    info.push("<strong>" + k + ":</strong> " + v);
                });
                addMessage(id + ' joined at ' + when + '<br />' + info.join('; '), 'text-success');
            },
            rejoined: function (id, when) {
                addMessage(id + ' reconnected at ' + when, 'text-warning');
            },
            left: function (id, when) {
                addMessage(id + ' left at ' + when, 'text-error');
            },
            invoked: function (id, when) {
                addMessage(id + ' invoked hub method at' + when, 'text-info');
            }
        });
    });

    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: activeTransport, jsonp: isJsonp }).done(function () {
        $.each(hubs, function () {
            this.server.invokedFromClient().fail($.connection.hub.log);
        });
    });
});