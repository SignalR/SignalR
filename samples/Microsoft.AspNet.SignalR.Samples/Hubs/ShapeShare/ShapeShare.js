/// <reference path="../../Scripts/jquery-1.6.4.js" />
/// <reference path="../../Scripts/jquery-ui-1.8.12.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />
/// <reference path="../../Scripts/signalR.js" />

$(function () {
    'use strict';

    var shapeShare = $.connection.shapeShare;

    function changeShape(el) {
        shapeShare.server.changeShape($(el).data("shapeId"), el.offsetLeft, el.offsetTop || 0, el.clientWidth, el.clientHeight);
    }

    function makeShape(shape) {
        /// <returns type="jQuery" />
        var el;
        switch (shape.Type) {
            case "picture":
                el = $("<div><img src='" + shape.Src + "' /></div>");
                break;
            case "circle":
                el = $("<div />");
                break;
            case "square":
                el = $("<div />");
            case "rectangle":
            default:
                el = $("<div />");
                break;
        }
        el
            .css({
                width: shape.Width,
                height: shape.Height,
                left: shape.Location.X,
                top: shape.Location.Y
            })
            .addClass("shape")
            .addClass(shape.Type)
            .attr("id", "s-" + shape.ID)
            .data("shapeId", shape.ID)
            .append("<div class='user-info' id='u-" + shape.ID + "'>" + (shapeShare.state.user.Name === shape.ChangedBy.Name ? "" : "changed by " + shape.ChangedBy.Name) + "</div>");
        return el;
    }

    shapeShare.client.shapeAdded = function (shape) {
        makeShape(shape)
            .hide()
            .appendTo("#shapes")
            .draggable({
                containment: "parent",
                drag: function () {
                    changeShape(this);
                }
            })
            .resizable({
                handles: "all",
                containment: "parent",
                aspectRatio:
                    shape.Type === "picture" ||
                    shape.Type === "square" ||
                    shape.Type === "circle",
                minHeight: 50,
                minWidth: 50,
                autoHide: true,
                resize: function () {
                    changeShape(this);
                }
            })
            .fadeIn();
    };

    shapeShare.client.shapeChanged = function (shape) {
        $("#s-" + shape.ID).css({
            top: shape.Location.Y,
            left: shape.Location.X,
            width: shape.Width,
            height: shape.Height
        });

        $("#u-" + shape.ID)
            .text("changed by " + shape.ChangedBy.Name);
    };

    shapeShare.client.shapeDeleted = function (id) {
        $("#s-" + id).fadeOut(250, function () {
            $(this).remove();
        });
    };

    shapeShare.client.shapesDeleted = function (shapes) {
        $.each(shapes, function () {
            shapeShare.client.shapeDeleted(this.id);
        });
    };

    $('#toolbar')
        .delegate("menu[label=add] a", "click", function (e) {
            e.preventDefault();

            shapeShare.server.createShape(this.className.toLowerCase());

            //            switch (this.className.toLowerCase()) {
            //                case "square":
            //                    shapeShare.createShape("square");
            //                    break;
            //                case "picture":
            //                    shapeShare.createShape("picture");
            //                    break;
            //            }

            return false;
        })
        .delegate("a.delete", "click", function (e) {
            e.preventDefault();
            shapeShare.server.deleteAllShapes();
            return false;
        });

    $("#user").change(function () {
        shapeShare.server.changeUserName(shapeShare.state.user.Name, $(this).val()).done(function () {
            $.cookie("userName", shapeShare.state.user.Name, { expires: 30 })
            $("#user").val(shapeShare.state.user.Name);
        });
    });

    function load() {
        shapeShare.server.getShapes().done(function (shapes) {
            $.each(shapes, function () {
                shapeShare.client.shapeAdded(this);
            });
        });
    }

    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: activeTransport }, function () {
        shapeShare.server.join($.cookie("userName"))
                         .done(function () {
                             $.cookie("userName", shapeShare.state.user.Name, { expires: 30 });
                             $("#user").val(shapeShare.state.user.Name);
                             load();
                         });
    });

});