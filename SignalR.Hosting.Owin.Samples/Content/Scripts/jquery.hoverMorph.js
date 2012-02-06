/// <reference path="jquery-1.6.2.js" />
/// <reference path="jquery.transform.js" />
/// <reference path="jquery.color.js" />
/// <reference path="jquery.easing.1.3.js" />

(function ($) {
    /// <param name="$" type="jQuery" />

    $.fn.hoverMorph = function (debug) {

        this.each(function () {
            var el = this,
                $el = $(el),
                bg = $el.css("backgroundImage");

            if (bg && bg != "none") {
                $("<img class='bg' />")
                    .attr("src", bg.replace(/url\(\"/, "").replace(/\"\)/, "").replace(/url\(/, "").replace(/\)/, ""))
                    .css({ position: "absolute" })
                    .insertBefore($el.children().first())
                    .load(function () {
                        $(this).css({
                            top: function () {
                                return -(this.offsetHeight / 8).toFixed(0);
                            },
                            left: function () {
                                return -(this.offsetWidth / 8).toFixed(0);
                            }
                        });
                    });
                $el.css("backgroundImage", "none");
            }
            if (debug) {
                $("<div class='hoverMorphDebug'></div>")
                    .css({
                        position: "absolute",
                        bottom: 0,
                        background: "rgba(200,200,200,.5)",
                        width: "100%",
                        zIndex: 100
                    })
                    .appendTo($el);
            }
        });

        this
            .mousemove(function (e) {
                if ($(this).data("morphing") !== true)
                    return;

                var $el = $(this),
                    $bg = $el.find("img.bg"),
                    scaleX = this.offsetWidth / 2,
                    scaleY = this.offsetHeight / 2,
                    elMidX = this.offsetLeft + this.offsetWidth - scaleX,
                    elMidY = this.offsetTop + this.offsetHeight - scaleY,
                    cursorX = e.clientX - elMidX,
                    cursorY = e.clientY - elMidY,
                    maxSkew = 4,
                    maxRotate = 4,
                    maxTranslate = 15,
                    pcX = (cursorX / scaleX),
                    pcY = (cursorY / scaleY),
                /*scale = (1 + (.075 * (pos(pcY)))).toFixed(3),*/
                    scale = ($.easing.easeInQuad(0, Math.abs(pcY), 1, .07, 1.03)).toFixed(3),
                    rotate = (maxRotate * pcX).toFixed(2),
                /*skewX = (maxSkew * pcX * pcY).toFixed(2),
                skewY = (maxSkew * pcY * pcX).toFixed(2),*/
                    translateX = -(maxTranslate * pcX).toFixed(2),
                    translateY = -(maxTranslate * pcY).toFixed(2);

                //$el.css("transform", "skew(" + skewX + "deg, " + skewY + "deg)");
                $el.css("transform", "rotate(" + rotate + "deg) scale(" + scale + ")");
                $bg.css("transform", "rotate(" + -rotate + "deg) scale(" + 1 / scale + ") translate(" + translateX + "px, " + translateY + "px)");

                if (debug) {
                    $el.find("div.hoverMorphDebug").html(
                        "cursor:" + cursorX + "," + cursorY + "<br />" +
                        "translate:" + translateX + "," + translateY + "<br />" +
                        "rotate:" + rotate + "<br />" +
                        "scale:" + scale
                    );
                }
            })
            .hover(function () { // Over
                var $el = $(this);

                $el.data("morphing", true);
                $el.clearQueue().stop();

                if (!$el.data("origBgColor")) { // set on first entry only
                    $el.data("origBgColor", $el.css("backgroundColor"));
                }
                $el.animate({ backgroundColor: "#b3d9ff" }, 250);
                if (debug) {
                    $el.find("div.hoverMorphDebug").show();
                }
            }, function () { // Out
                var $el = $(this),
                    $bg = $el.find("img.bg");

                $el.data("morphing", false);

                if (debug) {
                    $el.find("div.hoverMorphDebug").hide();
                }

                $el.animate({
                    backgroundColor: $el.data("origBgColor"),
                    transform: "skewX(0deg) skewY(0deg) scale(1, 1) rotate(0deg)"
                }, 1000, "easeOutElastic");
                $bg.animate({
                    transform: "scale(1, 1) rotate(0deg) translate(0px, 0px)"
                }, 1000, "easeOutElastic");
            });

        return this;
    };

} (jQuery));