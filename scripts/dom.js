/*
    VEJIS DOM Module v0.1.0.3
    Just another module based on VEJIS 0.5.
    http://vejis.org/modules/dom

    This version is still preliminary and subject to change.
    
    Copyright 2012, VILIC VANE
    Licensed under the MIT license.
*/

module_("dom", function () {
    var dom = this;

    this.ready = function () {
        var isReady = document.readyState == "complete",
            queue = [];

        if (!isReady) {
            if (document.addEventListener) {
                document.addEventListener("DOMContentLoaded", ready, false);
                window.addEventListener("load", ready, false);
            }
            else if (document.attachEvent) {
                document.attachEvent("onreadystatechange", ready);
                window.attachEvent("onload", ready);
                var toplevel = false;
                try {
                    toplevel = window.frameElement == null;
                } catch (e) { }
                if (document.documentElement.doScroll && toplevel) {
                    doScrollCheck();
                }
                doScrollCheck();
            }
        }

        return _(params_(Function), function (handlers) {
            //Add one or more handlers to a queue that will be called when the page is dom ready. If the dom is ready when adding, the handlers will be called immediately.
            //handlers: handlers that to be called.

            for_(handlers, function (handler) {
                if (isReady) handler();
                else queue.push(handler);
            });
        });

        function ready() {
            if (isReady) return;
            isReady = true;

            for_(queue, function (fn) {
                fn();
            })

            queue.length = 0;
        }

        function doScrollCheck() {
            if (isReady) return;

            try {
                document.documentElement.doScroll("left");
            } catch (e) {
                setTimeout(doScrollCheck, 1);
                return;
            }

            ready();
        }
    } ();

    this.query = function () {
        var selectors = [
            {
                key: ' #',
                unique: true,
                get: function (rel, id) {
                    if (rel == document) {
                        var ele = document.getElementById(id);
                        if (ele)
                            return [ele];
                    }
                    else {
                        var eles = rel.getElementsByTagName('*');
                        for (var i = 0; i < eles.length; i++)
                            if (eles[i].id == id)
                                return [eles[i]];
                    }
                    return [];
                }
            },
            {
                key: ' .',
                unique: false,
                get: function (rel, className) {
                    var eles = rel.getElementsByTagName('*');
                    var rst = [];
                    for (var i = 0, ele; ele = eles[i]; i++)
                        if (dom.containsClass(ele, className))
                            rst.push(ele);
                    return rst;
                }
            },
            {
                key: '>#',
                unique: true,
                get: function (rel, id) {
                    var eles = rel.childNodes;
                    for (var i = 0, ele; ele = eles[i]; i++)
                        if (ele.id == id)
                            return [ele];
                    return [];
                }
            },
            {
                key: '>.',
                unique: false,
                get: function (rel, className) {
                    var eles = rel.childNodes;
                    var rst = [];
                    for (var i = 0, ele; ele = eles[i]; i++)
                        if (dom.containsClass(ele, className))
                            rst.push(ele);
                    return rst;
                }
            },
            {
                key: '#',
                unique: true,
                get: function (rel, id) {
                    if (rel.id == id)
                        return [rel];
                    else return [];
                }
            },
            {
                key: '.',
                get: function (rel, className) {
                    if (dom.containsClass(rel, className))
                        return [rel];
                    else return [];
                }
            },
            {
                key: ' ',
                unique: false,
                get: function (rel, tagName) {
                    return rel.getElementsByTagName(tagName);
                }
            },
            {
                key: '>',
                allowSpace: true,
                unique: false,
                get: function (rel, tagName) {
                    var eles = rel.childNodes;
                    var rst = [];
                    for (var i = 0, ele; ele = eles[i]; i++)
                        if (ele.tagName == tagName.toUpperCase())
                            rst.push(ele);
                    return rst;
                }
            }
        ];

        var fn = _(String, opt_(Object, document), function (selector, rel) {
            //It returns an element or an array (determined by the selector) of elements by CSS selector given.
            //selector: the CSS selector, supports only "#", ".", ">".
            //rel: the relative element for query.

            return query(selector, [rel]);
        });

        fn._(String, IList, query);

        return fn;

        function query(selector, rels) {
            //It returns an element or an array (determined by the selector) of elements by CSS selector given.
            //selector: the CSS selector, supports only "#", ".", ">".
            //rels: the relative elements for query, can be either an array or a node list.

            selector = ' ' + trim(selector).replace(/\s+/g, ' ');

            for (var i = 0, s; s = selectors[i]; i++)
                if (s.allowSpace)
                    selector = selector.replace(new RegExp(' ?(\\' + s.key.split('').join('\\') + ') ?', 'i'), '$1');

            var unique;

            var eles = getEles(selector, rels);

            if (unique) return eles[0];
            else return eles;

            function getEles(selector, rels) {
                var eles = [];

                var re = /^([\w-]+|\*)/i;
                for (var i = 0, s; s = selectors[i]; i++) {
                    var key = s.key;
                    if (selector.indexOf(key) == 0) {
                        selector = selector.substr(key.length);
                        var word = (selector.match(re) || [])[0];
                        if (!word) return [];
                        selector = selector.substr(word.length);

                        for (var j = 0, rel; rel = rels[j]; j++)
                            append(eles, s.get(rel, word));

                        if (s.unique != undefined)
                            unique = s.unique;

                        break;
                    }
                }

                return selector ? getEles(selector, eles) : eles;
            }

        }

        function append(arr, items) {
            main:
            for (var i = 0; i < items.length; i++) {
                for (var j = 0; j < arr.length; j++)
                    if (items[i] == arr[j])
                        continue main;
                arr.push(items[i]);
            }
        }
    } ();


    this.containsClass = _(Object, String, function (ele, className) {
        //Check whether an element has a certain class name.
        //ele: the target element.
        //className: the class name to be checked.

        return new RegExp('^(.*\\s)?' + className + '(\\s.*)?$').test(ele.className);
    }).as_(Boolean);

    this.addClass = _(Object, String, function (ele, className) {
        //Add a class name to an element. Returns true if it made a difference.
        //ele: the target element.
        //className: the class name to be added.

        if (!dom.containsClass(ele, className)) {
            ele.className = trim(ele.className + ' ' + className);
            return true;
        }
        return false;
    }).as_(Boolean);

    this.removeClass = _(Object, String, function (ele, className) {
        //Remove a class name from an element. Returns true if it made a difference.
        //ele: the target element.
        //className: the class name to be removed.

        var newCN = ele.className.replace(new RegExp('^(?:(.*)\\s)?' + className + '(\\s.*)?$'), '$1$2');
        if (ele.className != newCN) {
            ele.className = trim(newCN);
            return true;
        }
        return false;
    }).as_(Boolean);

    this.toggleClass = _(Object, String, opt_(nul_(Boolean)), function (ele, className, toggle) {
        //If the parameter toggle is true, it will add the class name to the element,
        //else if toggle is false, it will remove the class name.
        //If toggle is not specified, the class name to an element if the element doesn't has the class name.
        //Otherwise, remove the class name.
        //It returns true if finally it added the class name.
        //ele: the target element.
        //className: the class name to be toggled.
        //toggle: see description of this function.
        
        if (toggle == null)
            toggle = !dom.containsClass(ele, className);
        
        if (toggle)
            dom.addClass(ele, className);
        else
            dom.removeClass(ele, className);

        return toggle;
    }).as_(Boolean);

    this.replaceClass = _(Object, String, String, function (ele, oldClass, newClass) {
        //Replace the class name of the element. Returns true if the replacement actually happened.
        //ele: the target element.
        //oldClass: the old class name to be replaced.
        //newClass: the new class name.

        if (dom.removeClass(ele, oldClass)) {
            dom.addClass(ele, newClass);
            return true;
        }
        return false;
    }).as_(Boolean);

    /* dom operation */

    this.create = _(String, opt_(Boolean, false), function (html, forceArray) {
        //Create an element or an array of elements by the HTML given.
        //Returns an array if the number of root elements is more than one.
        //html: the HTML to create element(s).
        //forceArray: force to return an array even if there is only one element.

        var temp = document.createElement('div');
        temp.innerHTML = html;
        var nodes = temp.childNodes;
        if (forceArray || nodes.length > 1) {
            var eles = [];
            var node;
            while (node = nodes[0]) {
                eles.push(node);
                temp.removeChild(node);
            }
            return eles;
        }
        return nodes[0];
    });

    this.clearChildNodes = _(Object, function (ele) {
        //Remove all child nodes from an element.
        //ele: the target element.

        var nodes = ele.childNodes;
        while (nodes[0])
            ele.removeChild(nodes[0]);
    });

    this.contains = _(Object, Object, function (parent, child) {
        //Determine whether an element has a specified child element.
        //parent: the parent element.
        //child: the child element.

        do {
            if (child.parentNode == parent)
                return true;
        }
        while (child = child.parentNode);
        return false;
    }).as_(Boolean);

    this.remove = _(Object, function (ele) {
        //Remove an element from its parent if it has a parent. Returns true if the removement actually happened.
        //ele: the target element.

        if (ele.parentNode) {
            ele.parentNode.removeChild(ele);
            return true;
        }
        return false;
    }).as_(Boolean);

    /* style */

    this.setStyle = _(params_(Object), PlainObject, function (eles, styles) {
        //Set style of one or more elements.
        //eles: the target elements.
        //styles: style in object notation.

        for_(eles, function (ele) {
            forin_(styles, function (value, style) {
                ele.style[style] = value.toString();
                if (style == "opacity" && "filter" in ele.style)
                    ele.style.filter = "alpha(opacity=" + Math.round(value * 100) + ")";
            });
        });
    });

    this.createStyleSheet = _(String, function (cssText) {
        //Create a style sheet by CSS given.
        //cssText: the CSS text.

        if (document.createStyleSheet)
            document.createStyleSheet().cssText = cssText;
        else {
            var style = document.createElement('style');
            style.type = 'text/css';
            style.textContent = cssText;
            dom.query('head')[0].appendChild(style);
        }
    });

    this.hide = _(params_(Object), function (eles) {
        //Set display style of target elements to "none".
        //eles: the target elements.

        for_(eles, function (ele) {
            ele.style.display = "none";
        });
    });

    this.show = _(params_(Object), function (eles) {
        //Set display style of target elements to "inline" or "block".
        //eles: the target elements.

        for_(eles, function (ele) {
            ele.style.display =
            /^(AABBR|ACRONYM|B|BDO|BIG|BR|CITE|CODE|DFN|EM|I|IMG|INPUT|KBD|LABEL|Q|SAMP|SELECT|SMALL|SPAN|STRONG|SUB|SUP|TEXTAREA|TT|VAR)$/i.test(ele.tagName) ?
            "inline" :
            "block";
        });
    });

    /* event */
    var EventHandler = delegate_(Object, function (event) { });

    this.on =
    this.addEventListener = _(Object, String, EventHandler, function (ele, event, handler) {
        //Add event handler to an element.
        //ele: the target element.
        //event: event string, such as "click", "load".
        //handler: the event handler.
        return addEventListener([ele], [event], handler);
    });

    this.addEventListener._(IList, List(String), EventHandler, addEventListener);

    function addEventListener(eles, events, handler) {
        //Add event handler to elements.
        //eles: the target elements.
        //events: event strings, such as "click", "load".
        //handler: the event handler.
        for_(eles, events, function (ele, event) {
            if (ele.addEventListener) {
                ele.addEventListener(event, handler, false);
            }
            else if (ele.attachEvent) {
                ele.attachEvent("on" + event, function () {
                    var e = window.event;

                    e.relatedTarget = e.toElement;
                    e.target = e.srcElement;

                    e.preventDefault = function () {
                        e.returnValue = false;
                    };
                    e.stopPropagation = function () {
                        e.cancelBubble = true;
                    };

                    handler.call(this, e);
                });
            }
        });
    }

    this.removeEventListener = _(Object, String, EventHandler, function (ele, event, handler) {
        //Remove event handler from an element.
        //ele: the target element.
        //event: event string, such as "click", "load".
        //handler: the event handler.
        return removeEventListener([ele], [event], handler);
    });

    this.removeEventListener._(IList, List(String), EventHandler, removeEventListener);

    function removeEventListener(eles, events, handler) {
        //Remove event handler from elements.
        //eles: the target elements.
        //events: event strings, such as "click", "load".
        //handler: the event handler.
        for_(eles, events, function (ele, event) {
            if (ele.removeEventListener)
                ele.removeEventListener(event, handler, false);
            else if (ele.detachEvent)
                ele.detachEvent("on" + event, handler);
        });
    }
    
    /* common stuffs */

    function trim(s) {
        return s.replace(/^\s+|\s+$/g, "");
    }
});