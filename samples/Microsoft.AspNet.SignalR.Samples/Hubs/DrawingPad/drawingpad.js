/// <reference path="jquery-1.7.1.js" />
/// <reference path="jcanvas.js" />


(function ($) {
    // hack from http://stackoverflow.com/a/6180819/588868
    var leftButtonDown = false;
    $(document).mousedown(function (e) {
        // Left mouse button was pressed, set flag
        if (e.which === 1) leftButtonDown = true;
    });
    $(document).mouseup(function (e) {
        // Left mouse button was released, clear flag
        if (e.which === 1) leftButtonDown = false;
    });
    function tweakMouseMoveEvent(e) {
        // Check from jQuery UI for IE versions < 9
        if ($.browser.msie && !(document.documentMode >= 9) && !event.button) {
            leftButtonDown = false;
        }

        // If left button is not set, set which to 0
        // This indicates no buttons pressed
        if (e.which === 1 && !leftButtonDown) e.which = 0;
    }

    var methods = {
        init: function (options) {

            var settings = {
                backcolor: 'white',
                linecolor: 'black'
            };
            settings = $.extend(settings, options);

            return this.each(function () {
                var $this = $(this);

                // bind data structure
                var data = $this.data('drawingpad');

                if (!data) {
                    $(this).data('drawingpad', {
                        target: $this,
                        drawingpad: {
                            lastCoords: { X: -1, Y: -1 },
                            coords: { X: -1, Y: -1 }
                        },
                        settings: settings
                    });
                }

                // bind events
                $this.bind('mouseenter.drawingpad', methods.mouseenter);
                $this.bind('mousemove.drawingpad', methods.mousemove);
                // initial setup of underlying html element
                $this.drawingpad('clear');
                //methods.clear();
            });
        },
        destroy: function () {
            return this.each(function () {
                // unbind data structure
                $(window).unbind('.drawingpad');
                data.drawingpad.remove();
                $this.removeData('drawingpad');

                // unbind events
                $(window).unbind('mousemove.drawingpad', methods.mousemove);
                $(window).unbind('mousemove.drawingpad', methods.enter);
            })
        },
        mousemove: function (args) {
            tweakMouseMoveEvent(args);
            var $this = $(this);

            var data = $this.data('drawingpad').drawingpad;
            var settings = $this.data('drawingpad').settings;
            data.coords.X = args.offsetX;
            data.coords.Y = args.offsetY;

            if (args.which == 1) {
                var line =  {
                    From: {
                        X: data.lastCoords.X,
                        Y: data.lastCoords.Y
                    }, To: {
                        X: data.coords.X,
                        Y: data.coords.Y
                    },
                    Color: settings.linecolor
                };
                $this.drawingpad('line', line);
                $this.triggerHandler('line.drawingpad', line);
            }

            data.lastCoords.X = data.coords.X;
            data.lastCoords.Y = data.coords.Y;

        },
        mouseenter: function (args) {
            var $this = $(this);
            var data = $this.data('drawingpad').drawingpad;
            data.lastCoords.X = args.offsetX;
            data.lastCoords.Y = args.offsetY;
        },
        clear: function (options) {
            var $this = $(this);
            var data = $this.data('drawingpad');

            var actual = $.extend(data.settings, options);

            $this.drawRect({
                fillStyle: actual.backcolor,
                x: 0, y: 0,
                width: $this.width() - 1,
                height: $this.height() - 1,
                fromCenter: false
            });
        },
        line: function (data) {
            var $this = $(this);
            $this.drawLine({
                strokeStyle: data.Color,
                strokeWidth: 3,
                x1: data.From.X,
                y1: data.From.Y,
                x2: data.To.X,
                y2: data.To.Y
            });
        }
    };

    $.fn.drawingpad = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.tooltip');
        }
    };


})(jQuery);