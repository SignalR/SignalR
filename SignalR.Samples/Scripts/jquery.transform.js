/*
* transform: A jQuery cssHooks adding cross-browser 2d transform capabilities to $.fn.css() and $.fn.animate()
*
* limitations:
* - requires jQuery 1.4.3+
* - Should you use the *translate* property, then your elements need to be absolutely positionned in a relatively positionned wrapper **or it will fail in IE678**.
* - transformOrigin is not accessible
*
* latest version and complete README available on Github:
* https://github.com/louisremi/jquery.transform.js
*
* Copyright 2011 @louis_remi
* Licensed under the MIT license.
*
* This saved you an hour of work?
* Send me music http://www.amazon.co.uk/wishlist/HNTU0468LQON
*
*/
(function ($) {

    /*
    * Feature tests and global variables
    */
    var div = document.createElement('div'),
	divStyle = div.style,
	propertyName = 'transform',
	suffix = 'Transform',
	testProperties = [
		'O' + suffix,
		'ms' + suffix,
		'Webkit' + suffix,
		'Moz' + suffix,
    // prefix-less property
		propertyName
	],
	i = testProperties.length,
	supportProperty,
	supportMatrixFilter,
	propertyHook,
	propertyGet,
	rMatrix = /Matrix([^)]*)/;

    // test different vendor prefixes of this property
    while (i--) {
        if (testProperties[i] in divStyle) {
            $.support[propertyName] = supportProperty = testProperties[i];
            continue;
        }
    }
    // IE678 alternative
    if (!supportProperty) {
        $.support.matrixFilter = supportMatrixFilter = divStyle.filter === '';
    }
    // prevent IE memory leak
    div = divStyle = null;

    // px isn't the default unit of this property
    $.cssNumber[propertyName] = true;

    /*
    * fn.css() hooks
    */
    if (supportProperty && supportProperty != propertyName) {
        // Modern browsers can use jQuery.cssProps as a basic hook
        $.cssProps[propertyName] = supportProperty;

        // Firefox needs a complete hook because it stuffs matrix with 'px'
        if (supportProperty == 'Moz' + suffix) {
            propertyHook = {
                get: function (elem, computed) {
                    return (computed ?
                    // remove 'px' from the computed matrix
					$.css(elem, supportProperty).split('px').join('') :
					elem.style[supportProperty]
				)
                },
                set: function (elem, value) {
                    // remove 'px' from matrices
                    elem.style[supportProperty] = /matrix[^)p]*\)/.test(value) ?
					value.replace(/matrix((?:[^,]*,){4})([^,]*),([^)]*)/, 'matrix$1$2px,$3px') :
					value;
                }
            }
            /* Fix two jQuery bugs still present in 1.5.1
            * - rupper is incompatible with IE9, see http://jqbug.com/8346
            * - jQuery.css is not really jQuery.cssProps aware, see http://jqbug.com/8402
            */
        } else if (/^1\.[0-5](?:\.|$)/.test($.fn.jquery)) {
            propertyHook = {
                get: function (elem, computed) {
                    return (computed ?
					$.css(elem, supportProperty.replace(/^ms/, 'Ms')) :
					elem.style[supportProperty]
				)
                }
            }
        }
        /* TODO: leverage hardware acceleration of 3d transform in Webkit only
        else if ( supportProperty == 'Webkit' + suffix && support3dTransform ) {
        propertyHook = {
        set: function( elem, value ) {
        elem.style[supportProperty] = 
        value.replace();
        }
        }
        }*/

    } else if (supportMatrixFilter) {
        propertyHook = {
            get: function (elem, computed) {
                var elemStyle = (computed && elem.currentStyle ? elem.currentStyle : elem.style),
				matrix;

                if (elemStyle && rMatrix.test(elemStyle.filter)) {
                    matrix = RegExp.$1.split(',');
                    matrix = [
					matrix[0].split('=')[1],
					matrix[2].split('=')[1],
					matrix[1].split('=')[1],
					matrix[3].split('=')[1]
				];
                } else {
                    matrix = [1, 0, 0, 1];
                }
                matrix[4] = elemStyle ? elemStyle.left : 0;
                matrix[5] = elemStyle ? elemStyle.top : 0;
                return "matrix(" + matrix + ")";
            },
            set: function (elem, value, animate) {
                var elemStyle = elem.style,
				currentStyle,
				Matrix,
				filter;

                if (!animate) {
                    elemStyle.zoom = 1;
                }

                value = matrix(value);

                // rotate, scale and skew
                if (!animate || animate.M) {
                    Matrix = [
					"Matrix(" +
						"M11=" + value[0],
						"M12=" + value[2],
						"M21=" + value[1],
						"M22=" + value[3],
						"SizingMethod='auto expand'"
				].join();
                    filter = (currentStyle = elem.currentStyle) && currentStyle.filter || elemStyle.filter || "";

                    elemStyle.filter = rMatrix.test(filter) ?
					filter.replace(rMatrix, Matrix) :
					filter + " progid:DXImageTransform.Microsoft." + Matrix + ")";

                    // center the transform origin, from pbakaus's Transformie http://github.com/pbakaus/transformie
                    if ((centerOrigin = $.transform.centerOrigin)) {
                        elemStyle[centerOrigin == 'margin' ? 'marginLeft' : 'left'] = -(elem.offsetWidth / 2) + (elem.clientWidth / 2) + 'px';
                        elemStyle[centerOrigin == 'margin' ? 'marginTop' : 'top'] = -(elem.offsetHeight / 2) + (elem.clientHeight / 2) + 'px';
                    }
                }

                // translate
                if (!animate || animate.T) {
                    // We assume that the elements are absolute positionned inside a relative positionned wrapper
                    elemStyle.left = value[4] + 'px';
                    elemStyle.top = value[5] + 'px';
                }
            }
        }
    }
    // populate jQuery.cssHooks with the appropriate hook if necessary
    if (propertyHook) {
        $.cssHooks[propertyName] = propertyHook;
    }
    // we need a unique setter for the animation logic
    propertyGet = propertyHook && propertyHook.get || $.css;

    /*
    * fn.animate() hooks
    */
    $.fx.step.transform = function (fx) {
        var elem = fx.elem,
		start = fx.start,
		end = fx.end,
		split,
		pos = fx.pos,
		transform,
		translate,
		rotate,
		scale,
		skew,
		T = false,
		M = false,
		prop;
        translate = rotate = scale = skew = '';

        // fx.end and fx.start need to be converted to their translate/rotate/scale/skew components
        // so that we can interpolate them
        if (!start || typeof start === "string") {
            // the following block can be commented out with jQuery 1.5.1+, see #7912
            if (!start) {
                start = propertyGet(elem, supportProperty);
            }

            // force layout only once per animation
            if (supportMatrixFilter) {
                elem.style.zoom = 1;
            }

            // if the start computed matrix is in end, we are doing a relative animation
            split = end.split(start);
            if (split.length == 2) {
                // remove the start computed matrix to make animations more accurate
                end = split.join('');
                fx.origin = start;
                start = 'none';
            }

            // start is either 'none' or a matrix(...) that has to be parsed
            fx.start = start = start == 'none' ?
			{
			    translate: [0, 0],
			    rotate: 0,
			    scale: [1, 1],
			    skew: [0, 0]
			} :
			unmatrix(toArray(start));

            // fx.end has to be parsed and decomposed
            fx.end = end = ~end.indexOf('matrix') ?
            // bullet-proof parser
			unmatrix(matrix(end)) :
            // faster and more precise parser
			components(end);

            // get rid of properties that do not change
            for (prop in start) {
                if (prop == 'rotate' ?
				start[prop] == end[prop] :
				start[prop][0] == end[prop][0] && start[prop][1] == end[prop][1]
			) {
                    delete start[prop];
                }
            }
        }

        /*
        * We want a fast interpolation algorithm.
        * This implies avoiding function calls and sacrifying DRY principle:
        * - avoid $.each(function(){})
        * - round values using bitewise hacks, see http://jsperf.com/math-round-vs-hack/3
        */
        if (start.translate) {
            // round translate to the closest pixel
            translate = ' translate(' +
			((start.translate[0] + (end.translate[0] - start.translate[0]) * pos + .5) | 0) + 'px,' +
			((start.translate[1] + (end.translate[1] - start.translate[1]) * pos + .5) | 0) + 'px' +
		')';
            T = true;
        }
        if (start.rotate != undefined) {
            rotate = ' rotate(' + (start.rotate + (end.rotate - start.rotate) * pos) + 'rad)';
            M = true;
        }
        if (start.scale) {
            scale = ' scale(' +
			(start.scale[0] + (end.scale[0] - start.scale[0]) * pos) + ',' +
			(start.scale[1] + (end.scale[1] - start.scale[1]) * pos) +
		')';
            M = true;
        }
        if (start.skew) {
            skew = ' skew(' +
			(start.skew[0] + (end.skew[0] - start.skew[0]) * pos) + 'rad,' +
			(start.skew[1] + (end.skew[1] - start.skew[1]) * pos) + 'rad' +
		')';
            M = true;
        }

        // In case of relative animation, restore the origin computed matrix here.
        transform = fx.origin ?
		fx.origin + translate + skew + scale + rotate :
		translate + rotate + scale + skew;

        propertyHook && propertyHook.set ?
		propertyHook.set(elem, transform, { M: M, T: T }) :
		elem.style[supportProperty] = transform;
    };

    /*
    * Utility functions
    */

    // turns a transform string into its 'matrix(A,B,C,D,X,Y)' form (as an array, though)
    function matrix(transform) {
        transform = transform.split(')');
        var 
			trim = $.trim
        // last element of the array is an empty string, get rid of it
		, i = transform.length - 1
		, split, prop, val
		, A = 1
		, B = 0
		, C = 0
		, D = 1
		, A_, B_, C_, D_
		, tmp1, tmp2
		, X = 0
		, Y = 0
		;
        // Loop through the transform properties, parse and multiply them
        while (i--) {
            split = transform[i].split('(');
            prop = trim(split[0]);
            val = split[1];
            A_ = B_ = C_ = D_ = 0;

            switch (prop) {
                case 'translateX':
                    X += parseInt(val, 10);
                    continue;

                case 'translateY':
                    Y += parseInt(val, 10);
                    continue;

                case 'translate':
                    val = val.split(',');
                    X += parseInt(val[0], 10);
                    Y += parseInt(val[1] || 0, 10);
                    continue;

                case 'rotate':
                    val = toRadian(val);
                    A_ = Math.cos(val);
                    B_ = Math.sin(val);
                    C_ = -Math.sin(val);
                    D_ = Math.cos(val);
                    break;

                case 'scaleX':
                    A_ = val;
                    D_ = 1;
                    break;

                case 'scaleY':
                    A_ = 1;
                    D_ = val;
                    break;

                case 'scale':
                    val = val.split(',');
                    A_ = val[0];
                    D_ = val.length > 1 ? val[1] : val[0];
                    break;

                case 'skewX':
                    A_ = D_ = 1;
                    C_ = Math.tan(toRadian(val));
                    break;

                case 'skewY':
                    A_ = D_ = 1;
                    B_ = Math.tan(toRadian(val));
                    break;

                case 'skew':
                    A_ = D_ = 1;
                    val = val.split(',');
                    C_ = Math.tan(toRadian(val[0]));
                    B_ = Math.tan(toRadian(val[1] || 0));
                    break;

                case 'matrix':
                    val = val.split(',');
                    A_ = +val[0];
                    B_ = +val[1];
                    C_ = +val[2];
                    D_ = +val[3];
                    X += parseInt(val[4], 10);
                    Y += parseInt(val[5], 10);
            }
            // Matrix product
            tmp1 = A * A_ + B * C_;
            B = A * B_ + B * D_;
            tmp2 = C * A_ + D * C_;
            D = C * B_ + D * D_;
            A = tmp1;
            C = tmp2;
        }
        return [A, B, C, D, X, Y];
    }

    // turns a matrix into its rotate, scale and skew components
    // algorithm from http://hg.mozilla.org/mozilla-central/file/7cb3e9795d04/layout/style/nsStyleAnimation.cpp
    function unmatrix(matrix) {
        var 
			scaleX
		, scaleY
		, skew
		, A = matrix[0]
		, B = matrix[1]
		, C = matrix[2]
		, D = matrix[3]
		;

        // Make sure matrix is not singular
        if (A * D - B * C) {
            // step (3)
            scaleX = Math.sqrt(A * A + B * B);
            A /= scaleX;
            B /= scaleX;
            // step (4)
            skew = A * C + B * D;
            C -= A * skew;
            D -= B * skew;
            // step (5)
            scaleY = Math.sqrt(C * C + D * D);
            C /= scaleY;
            D /= scaleY;
            skew /= scaleY;
            // step (6)
            if (A * D < B * C) {
                //scaleY = -scaleY;
                //skew = -skew;
                A = -A;
                B = -B;
                skew = -skew;
                scaleX = -scaleX;
            }

            // matrix is singular and cannot be interpolated
        } else {
            rotate = scaleX = scaleY = skew = 0;
        }

        return {
            translate: [+matrix[4], +matrix[5]],
            rotate: Math.atan2(B, A),
            scale: [scaleX, scaleY],
            skew: [skew, 0]
        }
    }

    // parse tranform components of a transform string not containing 'matrix(...)'
    function components(transform) {
        // split the != transforms
        transform = transform.split(')');

        var translate = [0, 0],
    rotate = 0,
    scale = [1, 1],
    skew = [0, 0],
    i = transform.length - 1,
    trim = $.trim,
    split, name, value;

        // add components
        while (i--) {
            split = transform[i].split('(');
            name = trim(split[0]);
            value = split[1];

            if (name == 'translateX') {
                translate[0] += parseInt(value, 10);

            } else if (name == 'translateY') {
                translate[1] += parseInt(value, 10);

            } else if (name == 'translate') {
                value = value.split(',');
                translate[0] += parseInt(value[0], 10);
                translate[1] += parseInt(value[1] || 0, 10);

            } else if (name == 'rotate') {
                rotate += toRadian(value);

            } else if (name == 'scaleX') {
                scale[0] *= value;

            } else if (name == 'scaleY') {
                scale[1] *= value;

            } else if (name == 'scale') {
                value = value.split(',');
                scale[0] *= value[0];
                scale[1] *= (value.length > 1 ? value[1] : value[0]);

            } else if (name == 'skewX') {
                skew[0] += toRadian(value);

            } else if (name == 'skewY') {
                skew[1] += toRadian(value);

            } else if (name == 'skew') {
                value = value.split(',');
                skew[0] += toRadian(value[0]);
                skew[1] += toRadian(value[1] || '0');
            }
        }

        return {
            translate: translate,
            rotate: rotate,
            scale: scale,
            skew: skew
        };
    }

    // converts an angle string in any unit to a radian Float
    function toRadian(value) {
        return ~value.indexOf('deg') ?
		parseInt(value, 10) * (Math.PI * 2 / 360) :
		~value.indexOf('grad') ?
			parseInt(value, 10) * (Math.PI / 200) :
			parseFloat(value);
    }

    // Converts 'matrix(A,B,C,D,X,Y)' to [A,B,C,D,X,Y]
    function toArray(matrix) {
        // Fremove the unit of X and Y for Firefox
        matrix = /\(([^,]*),([^,]*),([^,]*),([^,]*),([^,p]*)(?:px)?,([^)p]*)(?:px)?/.exec(matrix);
        return [matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], matrix[6]];
    }

    $.transform = {
        centerOrigin: 'margin'
    };

})(jQuery);