require_("scripts/", ["dom.js", "dom.animation.js", "base64.js"]);

var loaded = false;

window.onload = function () {
    loaded = true;

    var main = import_("main");
    if (main) main.load();

    //Google Analytics
    var _gaq = _gaq || [];
    _gaq.push(['_setAccount', 'UA-36452018-1']);
    _gaq.push(['_setDomainName', 'x-wall.org']);
    _gaq.push(['_trackPageview']);

    (function () {
        var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
        ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
        var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
    })();
};

use_("dom", "dom.animation", function (dom, anim) {
    var contentRoot = "contents/";

    var style = {
        transparent: {
            opacity: 0
        },
        opaque: {
            opacity: 1
        }
    };

    var version;

    module_("main", function () {
        this.load = function () {
            var xhr = new XMLHttpRequest();
            xhr.open("get", contentRoot + lang.name.toLowerCase() + "/html");
            xhr.setRequestHeader("If-Modified-Since", "0");
            xhr.send(null);

            xhr.onreadystatechange = function () {
                if (xhr.readyState == 4 && xhr.status == 200) {
                    use_("base64", function (base64) {
                        var html = base64.decode(xhr.responseText);
                        loadContent(html);
                    });
                }
            };
        };

        if (loaded)
            this.load();
    });

    fetchVersion();

    function fetchVersion() {
        var xhr = new XMLHttpRequest();
        xhr.open("get", "release/version");
        xhr.setRequestHeader("If-Modified-Since", "0");
        xhr.send(null);

        xhr.onreadystatechange = function () {
            if (xhr.readyState == 4 && xhr.status == 200) {
                version = xhr.responseText;
                var versionSpan = dom.query("#version");
                if (versionSpan)
                    versionSpan.innerHTML = version;
            }
        };
    }

    function loadContent(html) {
        document.body.innerHTML = html;

        var title = dom.query("#title").getAttribute("data-document-title");
        document.title = title;

        if (version)
            dom.query("#version").innerHTML = version;

        var header = dom.query("#header-wrapper");
        var main = dom.query("#main-wrapper");

        var headerAnim = new anim.Element(header, style.transparent, 500);
        var mainAnim = new anim.Element(main, style.transparent, 500);

        headerAnim.setStyle(style.opaque);
        mainAnim.setStyle(style.opaque);

        var imgs = dom.query("#content-wrapper img");

        for_(imgs, function (img) {
            var imgAnim = new anim.Element(img, style.transparent, 500);

            if (img.complete) onload();
            else img.onload = onload;

            function onload() {
                imgAnim.setStyle(style.opaque);
            }
        });
    } //end of loadContent
});