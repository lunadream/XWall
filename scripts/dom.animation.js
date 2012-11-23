/// <reference path="dom.js" />
/*
    VEJIS DOM Animation Module v0.1.0.1
    Just another module based on VEJIS 0.5.
    http://vejis.org/modules/dom

    This version is still preliminary and subject to change.
    
    Copyright 2012, VILIC VANE
    Licensed under the MIT license.
*/

use_("dom", function (dom) {
    module_("dom.animation", function () {
        var Transition =
        this.Transition = delegate_(Number, function (x) {
            //Returns y based on x given.
            //x: the x of the transition curve.
        }).as_(Number);

        Transition.easeInOut = function (x) { return (1 - Math.cos(x * Math.PI)) / 2; };
        Transition.easeIn = function (x) { return 1 - Math.cos(x * Math.PI / 2); };
        Transition.easeOut = function (x) { return Math.sin(x * Math.PI / 2); };
        Transition.linear = function (x) { return x; };

        this.class_("Element", function () {
            var interval = 20; //20ms
            var timer;

            var start = {};
            var current = {};
            var end = {};

            var ele;
            var dur; //duration
            var passed; //passed time
            var trans;
            var defaultTransition;
            var defaultDuration;

            var cb; //callback

            this._(Object, opt_(PlainObject, {}), opt_(Number, 1000), opt_(Transition, Transition.linear),
            function (element, initStyle, duration, transition) {
                //Create an animation element.
                //element: the target HTML element.
                //initStyle: initial style object.
                //duration: duration in millisecond.
                //transition: an function, defined a curve describing how a transition goes.

                ele = element;

                this.initStyle(initStyle);

                defaultTransition = transition;
                defaultDuration = duration;
            });

            this.initStyle = _(PlainObject, function (style) {
                clearInterval(timer);
                forin_(style, function (value, i) {
                    current[i] = new Value(value);
                    start[i] = new Value(value);
                    end[i] = new Value(value);
                });
                dom.setStyle(ele, start);
            });

            this.setStyle = _(PlainObject, opt_(nul_(Function)), opt_(Number), opt_(Transition, Transition.linear),
            function (style, callback, duration, transition) {
                //Set styles for an element.
                //style: a PlainObject describing the target style.
                //callback: callback that will be called when the animation complete.
                //duration: duration in millisecond.
                //transition: an function, defined a curve describing how a transition goes.

                return setStyle([style], callback, duration, transition);
            });

            this.setStyle._(List(PlainObject), opt_(nul_(Function)), opt_(Number), opt_(Transition, Transition.linear), setStyle);

            function setStyle(styles, callback, duration, transition) {
                //Set styles for an element.
                //styles: an array of PlainObject describing the target styles.
                //callback: callback that will be called when the animation complete.
                //duration: duration in millisecond.
                //transition: an function, defined a curve describing how a transition goes.

                clearInterval(timer);
                cb = nextStyle;
                dur = duration || defaultDuration;
                trans = transition || defaultTransition;

                var now = 0;
                nextStyle();

                function nextStyle() {
                    var style = styles[now++];
                    if (!style) {
                        if (callback) callback();
                        return;
                    }

                    forin_(start, function (value, i) {
                        current[i].copyTo(start[i]);
                        (style.hasOwnProperty(i) ? new Value(style[i]) : start[i]).copyTo(end[i]);
                    });

                    passed = 0;
                    timer = setInterval(next, interval);
                }
            }

            function next() {
                passed += interval;

                if (passed >= dur) {
                    clearInterval(timer);
                    passed = dur;
                    if (cb) cb();
                }

                var x = passed / dur;

                forin_(start, function (sValue, i) {
                    //var sValue = start[i];
                    var eValue = end[i];
                    current[i].computeValue(sValue, eValue, trans(x));
                });

                dom.setStyle(ele, current);
            }
        });

        function Value(init) {
            var isRGB = false;

            if (arguments.length > 0) {
                var re = /^(?:#((?:[\da-f]{3}){1,2})|(-?[\d\.]+)(.*))$/i;
                var groups = re.exec(init);

                if (!groups)
                    return error("unrecognized value");

                if (groups[1]) {
                    isRGB = true;
                    this.number = new RGB(groups[1]);
                    this.unit = "";
                }
                else {
                    this.number = Number(groups[2]);
                    this.unit = groups[3];
                }
            }
            else {
                this.number = 0;
                this.unit = "";
            }

            this.computeValue = function (start, end, pos) {
                if (isRGB) {
                    var colors = this.number.colors;
                    for (var i = 0; i < colors.length; i++)
                        colors[i] = start.number.colors[i] + (end.number.colors[i] - start.number.colors[i]) * pos;
                }
                else
                    this.number = start.number + (end.number - start.number) * pos;
            };

            this.copyTo = function (to) {
                to.unit = this.unit;
                if (isRGB) {
                    var tcs = to.number.colors;
                    for_(this.number.colors, function (c, i) {
                        tcs[i] = c;
                    });
                }
                else to.number = this.number;
            };

            this.toString = function () {
                return this.number.toString() + this.unit;
            };
        }

        function RGB(rgb) {
            this.colors = [];
            if (rgb.length == 3)
                for (var i = 0; i < 3; i++) {
                    var n = rgb.charAt(i);
                    this.colors[i] = parseInt(n + n, 16);
                }
            else if (rgb.length == 6)
                for (var i = 0; i < 3; i++)
                    this.colors[i] = parseInt(rgb.substr(i * 2, 2), 16);
            this.toString = function () {
                var rgb = "#";
                for_(this.colors, function (c) {
                    var hex = Math.round(c).toString(16);
                    rgb += hex.length == 1 ? "0" + hex : hex;
                });
                return rgb;
            };
        }
    });
});
