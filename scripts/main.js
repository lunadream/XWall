/// <reference path="../templates/page.html" />
/// <reference path="dom.js" />
/// <reference path="dom.animation.js" />

use_("dom", "dom.animation", function (dom, anim) {
    module_("xwall.index/main", function () {
        var main = this;

        // VERSION SPAN
        (function () {
            var xhr = this.XMLHttpRequest ? new XMLHttpRequest() : new ActiveXObject("MSXML2.XMLHTTP");
            xhr.open("GET", main.root + "/release/version");
            xhr.setRequestHeader("If-Modified-Since", "0");
            xhr.send(null);

            xhr.onreadystatechange = function () {
                if (xhr.readyState == 4 && xhr.status == 200) {
                    var version = xhr.responseText;
                    var versionSpan = document.getElementById("version");
                    versionSpan.innerHTML = version;
                }
            };
        })();

        (function () {
            var body = new anim.Element(document.body, {}, 200, anim.Transition.easeOut);
            var docEle = new anim.Element(document.documentElement, {}, 200, anim.Transition.easeOut);
            var all = dom.query("a");

            for_(all, function (link) {
                var id = (link.href.match(/#.*/) || [])[0];
                var anchor = id ? id == "#" ? document.body : dom.query(id) : null;
                if (!anchor) return;

                dom.addEventListener(link, "click", function (e) {
                    var initStyle = { scrollTop: document.body.scrollTop || document.documentElement.scrollTop };
                    var targetStyle = { scrollTop: anchor.getBoundingClientRect().top + (document.body.scrollTop || document.documentElement.scrollTop) };

                    body.initStyle(initStyle);
                    docEle.initStyle(initStyle);
                    body.setStyle(targetStyle);
                    docEle.setStyle(targetStyle);
                    e.preventDefault();
                });
            });
        })();

        // MAIN MENU
        (function () {
            var wrapperEle = dom.create(
                '<div id="main-menu-list-wrapper">' +
                    "<b><i></i></b>" +
                    '<ul id="main-menu-list"></ul>' +
                "</div>"
            );
            var listEle = dom.query("#main-menu-list", wrapperEle);
            var arrowEle = dom.query("b", wrapperEle)[0];

            var header = dom.query("#header");
            header.appendChild(wrapperEle);

            var current;
            var timer;

            var wrapper = new anim.Element(wrapperEle, {}, 200);
            var arrow = new anim.Element(arrowEle, {}, 100);
            var list = new anim.Element(listEle, {}, 100);

            var nav = dom.query("#nav");
            var menuItems = dom.query("> li", nav);
            for_(menuItems, function (item) {
                var ul = dom.query("ul", item)[0];

                ul.parentNode.removeChild(ul);

                var links = dom.query("li", ul);
                if (!links.length) return;

                var mouseover = false;
                dom.addEventListener(item, "mouseover", function () {
                    if (mouseover) return;
                    mouseover = true;

                    clearTimeout(timer);

                    if (current == null)
                        dom.show(wrapperEle);

                    dom.clearChildNodes(listEle);
                    var width = 120;
                    var height = 0;

                    for_(links, function (link) {
                        listEle.appendChild(link);
                        var a = dom.query("a", link)[0];
                        a.className = "adjusting";
                        width = Math.max(a.offsetWidth, width);
                        a.className = "";
                        height += link.offsetHeight;
                    });

                    var offsetRight = header.offsetWidth - (nav.offsetLeft + item.offsetLeft + item.offsetWidth);

                    var top = nav.offsetTop + item.offsetHeight;

                    var right = Math.max(offsetRight + item.offsetWidth + 10 - (width + 2), 0);
                    var arrowRight = offsetRight + item.offsetWidth / 2 - right;

                    if (current == null) {
                        wrapper.initStyle({
                            top: top + "px",
                            right: right + "px",
                            opacity: 0
                        });

                        arrow.initStyle({
                            right: arrowRight + "px"
                        });

                        list.initStyle({
                            width: width + "px", height: "0px"
                        });
                    }

                    current = item;

                    wrapper.setStyle({
                        right: right + "px",
                        opacity: 1
                    });

                    arrow.setStyle({
                        right: arrowRight + "px"
                    });

                    list.setStyle({
                        width: width + "px",
                        height: height + "px"
                    }, function () { }, 150);
                });

                dom.addEventListener(item, "mouseout", onitemmouseout);
                dom.addEventListener(wrapperEle, "mouseout", onmouseout);
                dom.addEventListener([item, wrapperEle], ["click"], hideMenu);

                dom.addEventListener(wrapperEle, "mouseover", function () {
                    clearTimeout(timer);
                });

                function onitemmouseout(e) {
                    if (e.relatedTarget && dom.contains(this, e.relatedTarget))
                        return;

                    mouseover = false;
                    clearTimeout(timer);
                    timer = setTimeout(hideMenu, 100);
                }

                function onmouseout(e) {
                    if (e.relatedTarget && dom.contains(this, e.relatedTarget))
                        return;

                    clearTimeout(timer);
                    timer = setTimeout(hideMenu, 100);
                }
            });

            function hideMenu() {
                clearTimeout(timer);

                wrapper.setStyle({
                    opacity: 0
                }, done, 200);

                list.setStyle({
                    height: "0px"
                }, done, 200);

                var count = 2;

                function done() {
                    count--;
                    if (!count) {
                        dom.hide(wrapperEle);
                        current = null;
                    }
                }
            }

        })();
    });
});