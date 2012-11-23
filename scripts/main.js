require_("scripts/", ["dom.js", "dom.animation.js"]);

use_("dom", "dom.animation", function (dom, anim) {
    var lang = function () {
        //var contentRoot = "contents/";
        var contentRoot = "https://raw.github.com/vilic/x-wall/gh-pages/contents/";

        var langs = [
            {
                name: "en-US",
                title: "X-WALL",
                loading: "loading...",
                font: "Segoe UI, Arial",
                file: contentRoot + "en-us.js"
            },
            {
                name: "zh-CN",
                title: "X-WALL",
                loading: "正在加载...",
                font: "微软雅黑, 黑体",
                file: contentRoot + "zh-cn.js"
            }
        ];

        var langName =
            navigator.language ||
            navigator.userLanguage ||
            navigator.browserLanguage ||
            navigator.systemLanguage;

        return getLang(langName) || getLang(langName.substr(0, 2)) || langs[0];

        function getLang(name) {
            for (var i = 0; i < langs.length; i++) {
                if (langs[i].name.indexOf(name) == 0)
                    return langs[i];
            }
        }
    }();

    var style = {
        transparent: {
            opacity: 0
        },
        opaque: {
            opacity: 1
        }
    };
    
    module_("main", function () {
        this.loadContent = function (html) {
            document.body.innerHTML = html;

            var title = dom.query("#title").getAttribute("data-document-title");
            document.title = title;

            var header = dom.query("#header-wrapper");
            var main = dom.query("#main-wrapper");

            var headerAnim = new anim.Element(header, style.transparent, 500);
            var mainAnim = new anim.Element(main, style.transparent, 500);

            headerAnim.setStyle(style.opaque);
            mainAnim.setStyle(style.opaque);
        }; //end of loadContent
    });
    
    dom.ready(function () {
        document.title = lang.title;
        document.body.style.fontFamily = lang.font;

        var loading = dom.query("#loading");
        loading.innerHTML = lang.loading;

        var loadingAnim = new anim.Element(loading, style.transparent, 500);
        loadingAnim.setStyle(style.opaque);

        require_(lang.file);

        //var xhr = new XMLHttpRequest();
        //xhr.open("get", lang.file);
        //xhr.send(null);

        //xhr.onreadystatechange = function () {
        //    if (xhr.readyState == 4 && xhr.status == 200) {
        //        loadingAnim.setStyle(style.transparent, function () {
        //            loadContent(xhr.responseText);
        //        });
        //    }
        //};
    });
});