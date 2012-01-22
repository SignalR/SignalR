/// <reference path="../../Scripts/jquery-1.6.2.js" />
/// <reference path="../../Scripts/jquery-ui-1.8.12.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />
/// <reference path="../../Scripts/signalR.js" />

$(function () {
    'use strict';

    var shapeShare = $.connection.shapeShare;

    function changeShape(el) {
        shapeShare.changeShape($(el).data("shapeId"), el.offsetLeft, el.offsetTop || 0, el.clientWidth, el.clientHeight);
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
            .append("<div class='user-info' id='u-" + shape.ID + "'>" + (shapeShare.user.Name === shape.ChangedBy.Name ? "" : "changed by " + shape.ChangedBy.Name) + "</div>");
        return el;
    }

    shapeShare.shapeAdded = function (shape) {
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

    shapeShare.shapeChanged = function (shape) {
        if (this.user.ID === shape.ChangedBy.ID) {
            $("#u-" + shape.ID).text("");
            return;
        }

        $("#s-" + shape.ID).css({
            top: shape.Location.Y,
            left: shape.Location.X,
            width: shape.Width,
            height: shape.Height
        });

        $("#u-" + shape.ID)
            .text("changed by " + shape.ChangedBy.Name);
    };

    shapeShare.shapeDeleted = function (id) {
        $("#s-" + id).fadeOut(250, function () {
            $(this).remove();
        });
    };

    shapeShare.shapesDeleted = function (shapes) {
        $.each(shapes, function () {
            shapeShare.shapeDeleted(this.id);
        });
    };

    $('#toolbar')
        .delegate("menu[label=add] a", "click", function (e) {
            e.preventDefault();

            shapeShare.createShape(this.className.toLowerCase());

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
            shapeShare.deleteAllShapes();
            return false;
        });

    $("#user").change(function () {
        shapeShare.changeUserName(shapeShare.user.Name, $(this).val(), function () {
            $.cookie("userName", shapeShare.user.Name, { expires: 30 })
            $("#user").val(shapeShare.user.Name);
        });
    });

    function load() {
        shapeShare.getShapes(function (shapes) {
            $.each(shapes, function () {
                shapeShare.shapeAdded(this);
            });
        });
    }

    $.connection.hub.start({ transport: activeTransport }, function () {
        shapeShare.join($.cookie("userName"), function () {
            $.cookie("userName", shapeShare.user.Name, { expires: 30 });
            $("#user").val(shapeShare.user.Name);
            load();
        });
    });

});