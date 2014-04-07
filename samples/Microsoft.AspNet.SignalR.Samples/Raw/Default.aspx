<%@ Page Title="ASP.NET SignalR: Connection API" Language="C#" MasterPageFile="~/SignalR.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Microsoft.AspNet.SignalR.Samples.Raw.Default" %>

<asp:Content runat="server" ContentPlaceHolderID="MainContent">
    <ul class="breadcrumb">
        <li><a href="<%: ResolveUrl("~/") %>">SignalR Samples</a> <span class="divider">/</span></li>
        <li class="active">Connection API</li>
    </ul>

    <div class="page-header">
        <h2>Connection API <small>Low-level connection abstraction</small></h2>
        <p>Demonstrates all features of the lower-level connection API including starting and stopping, sending and
           receiving messages, and managing groups.</p>
    </div>

    <a href="crossdomain.htm">Cross Domain</a>
    
    <h4>To Everybody</h4>
    <form class="form-inline">
        <div class="input-append">
            <input type="text" id="msg" placeholder="Type a message, name or group" />
            <input type="button" id="broadcast" class="btn" value="Broadcast" />
            <input type="button" id="broadcast-exceptme" class="btn" value="Broadcast (All Except Me)" />
            <input type="button" id="join" class="btn" value="Enter Name" />
            <input type="button" id="join-group" class="btn" value="Join Group" />
            <input type="button" id="leave-group" class="btn" value="Leave Group" />
        </div>
    </form>
    
    <h4>To Me</h4>
    <form class="form-inline">
        <div class="input-append">
            <input type="text" id="me" placeholder="Type a message" />
            <input type="button" id="send" class="btn" value="Send to me" />
        </div>
    </form>

    <h4>Private Message</h4>
    <form class="form-inline">
        <div class="input-prepend input-append">
            <input type="text" name="message" id="message" placeholder="Type a message" />
            <input type="text" name="user" id="user" placeholder="Type a user or group name" />
        
            <input type="button" id="privatemsg" class="btn" value="Send to user" />
            <input type="button" id="groupmsg" class="btn" value="Send to group" />
        </div>
    </form>

    <button id="stopStart" class="btn btn-info btn-small" disabled="disabled"><i class="icon-stop icon-white"></i> <span>Stop Connection</span></button>

    <h4>Messages</h4>
    <ul id="messages">
    </ul>
</asp:Content>

<asp:Content runat="server" ContentPlaceHolderID="Scripts">
    <script src="<%: ResolveUrl("~/Scripts/jquery.cookie.js") %>"></script>
    <script>
        $(function () {
            "use strict";

            var connection = $.connection("../raw-connection");
            connection.logging = true;

            connection.received(function (data) {
                $("<li/>").html(window.JSON.stringify(data)).appendTo($("#messages"));
                if (data.type == 2) {
                    $.cookie('user', data.data);
                }
            });

            connection.reconnected(function () {
                $("<li/>").css("background-color", "green")
                          .css("color", "white")
                          .html("[" + new Date().toTimeString() + "]: Connection re-established")
                          .appendTo($("#messages"));
            });

            connection.error(function (err) {
                $("<li/>").html(err || "Error occurred")
                          .appendTo($("#messages"));
            });

            connection.disconnected(function () {
                $("#stopStart")
                    .prop("disabled", false)
                    .find("span")
                        .text("Start Connection")
                        .end()
                    .find("i")
                        .removeClass("icon-stop")
                        .addClass("icon-play");
            });

            connection.stateChanged(function (change) {
                var oldState = null,
                    newState = null;
                for (var p in $.signalR.connectionState) {
                    if ($.signalR.connectionState[p] === change.oldState) {
                        oldState = p;
                    }

                    if ($.signalR.connectionState[p] === change.newState) {
                        newState = p;
                    }
                }

                $("<li/>").html(oldState + " => " + newState)
                          .appendTo($("#messages"));
            });

            // Uncomment this block to enable custom JSON parser
            //connection.json = {
            //    parse: function (text, reviver) {
            //        console.log("Parsing JSON");
            //        return window.JSON.parse(text, reviver);
            //    },
            //    stringify: function (value, replacer, space) {
            //        return window.JSON.stringify(value, replacer, space);
            //    }
            //};

            var start = function () {
                connection.start({ transport: activeTransport, jsonp: isJsonp })
                    .then(function () {
                        $("#stopStart")
                           .prop("disabled", false)
                           .find("span")
                               .text("Stop Connection")
                               .end()
                           .find("i")
                               .removeClass("icon-play")
                               .addClass("icon-stop");
                    });
            };
            start();

            $("#send").click(function () {
                connection.send({ type: 0, value: $("#me").val() });
            });

            $("#broadcast").click(function () {
                connection.send({ type: 1, value: $("#msg").val() });
            });

            $("#broadcast-exceptme").click(function () {
                connection.send({ type: 7, value: $("#msg").val() });
            });

            $("#join").click(function () {
                connection.send({ type: 2, value: $("#msg").val() });
            });

            $("#privatemsg").click(function () {
                connection.send({ type: 3, value: $("#user").val() + "|" + $("#message").val() });
            });

            $('#join-group').click(function () {
                connection.send({ type: 4, value: $("#msg").val() });
            });

            $('#leave-group').click(function () {
                connection.send({ type: 5, value: $("#msg").val() });
            });

            $("#groupmsg").click(function () {
                connection.send({ type: 6, value: $("#user").val() + "|" + $("#message").val() });
            });

            $("#stopStart").click(function () {
                var $el = $(this);

                $el.prop("disabled", true);

                if ($.trim($el.find("span").text()) === "Stop Connection") {
                    connection.stop();
                } else {
                    start();
                }
            });
        });
    </script>
</asp:Content>
