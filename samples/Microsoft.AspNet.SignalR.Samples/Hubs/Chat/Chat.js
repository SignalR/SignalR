/// <reference path="../../Scripts/jquery-1.6.4.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />

$(function () {
    var chat = $.connection.chat;

    function clearMessages() {
        $('#messages').html('');
    }

    function refreshMessages() { refreshList($('#messages')); }

    function clearUsers() {
        $('#users').html('');
    }

    function refreshUsers() { refreshList($('#users')); }

    function refreshList(list) {
        if (list.is('.ui-listview')) {
            list.listview('refresh');
        }
    }

    function addMessage(content, type) {
        var e = $('<li/>').html(content).appendTo($('#messages'));
        refreshMessages();

        if (type) {
            e.addClass(type);
        }
        updateUnread();
        e[0].scrollIntoView();
        return e;
    }

    chat.client.refreshRoom = function (room) {
        clearMessages();
        clearUsers();

        chat.server.getUsers()
            .done(function (users) {
                $.each(users, function () {
                    chat.client.addUser(this, true);
                });
                refreshUsers();

                $('#new-message').focus();
            });

        addMessage('Entered ' + room, 'notification');
    };

    chat.client.showRooms = function (rooms) {
        if (!rooms.length) {
            addMessage('No rooms available', 'notification');
        }
        else {
            $.each(rooms, function () {
                addMessage(this.Name + ' (' + this.Count + ')');
            });
        }
    };

    chat.client.addMessageContent = function (id, content) {
        var e = $('#m-' + id).append(content);
        refreshMessages();
        updateUnread();
        e[0].scrollIntoView();
    };

    chat.client.addMessage = function (id, name, message) {
        var data = {
            name: name,
            message: message,
            id: id
        };

        var e = $('#new-message-template').tmpl(data)
                                          .appendTo($('#messages'));
        refreshMessages();
        updateUnread();
        e[0].scrollIntoView();
    };

    chat.client.addUser = function (user, exists) {
        var id = 'u-' + user.Name;
        if (document.getElementById(id)) {
            return;
        }

        var data = {
            name: user.Name,
            hash: user.Hash
        };

        var e = $('#new-user-template').tmpl(data)
                                       .appendTo($('#users'));
        refreshUsers();

        if (!exists && this.state.name != user.Name) {
            addMessage(user.Name + ' just entered ' + this.state.room, 'notification');
            e.hide().fadeIn('slow');
        }

        updateCookie();
    };

    chat.client.changeUserName = function (oldUser, newUser) {
        $('#u-' + oldUser.Name).replaceWith(
                $('#new-user-template').tmpl({
                    name: newUser.Name,
                    hash: newUser.Hash
                })
        );
        refreshUsers();

        if (newUser.Name === this.state.name) {
            addMessage('Your name is now ' + newUser.Name, 'notification');
            updateCookie();
        }
        else {
            addMessage(oldUser.Name + '\'s nick has changed to ' + newUser.Name, 'notification');
        }
    };

    chat.client.sendMeMessage = function (name, message) {
        addMessage('*' + name + '* ' + message, 'notification');
    };

    chat.client.sendPrivateMessage = function (from, to, message) {
        addMessage('<emp>*' + from + '*</emp> ' + message, 'pm');
    };

    chat.client.leave = function (user) {
        if (this.state.id != user.Id) {
            $('#u-' + user.Name).fadeOut('slow', function () {
                $(this).remove();
            });

            addMessage(user.Name + ' left ' + this.state.room, 'notification');
        }
    };

    $('#send-message').submit(function () {
        var command = $('#new-message').val();

        chat.server.send(command)
            .fail(function (e) {
                addMessage(e, 'error');
            });

        $('#new-message').val('');
        $('#new-message').focus();

        return false;
    });

    $(window).blur(function () {
        chat.state.focus = false;
    });

    $(window).focus(function () {
        chat.state.focus = true;
        chat.state.unread = 0;
        document.title = 'SignalR Chat';
    });

    function updateUnread() {
        if (!chat.state.focus) {
            if (!chat.state.unread) {
                chat.state.unread = 0;
            }
            chat.state.unread++;
        }
        updateTitle();
    }

    function updateTitle() {
        if (chat.state.unread == 0) {
            document.title = 'SignalR Chat';
        }
        else {
            document.title = 'SignalR Chat (' + chat.state.unread + ')';
        }
    }

    function updateCookie() {
        $.cookie('userid', chat.state.id, { path: '/', expires: 30 });
    }

    addMessage('Welcome to the SignalR IRC clone', 'notification');

    $('#new-message').val('');
    $('#new-message').focus();

    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: activeTransport }, function () {
        chat.server.join()
            .done(function (success) {
                if (success === false) {
                    $.cookie('userid', '');
                    addMessage('Choose a name using "/nick nickname".', 'notification');
                }
                addMessage('After that, you can view rooms using "/rooms" and join a room using "/join roomname".', 'notification');
            });
    });
});