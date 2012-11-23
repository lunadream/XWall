import_("main").loadContent('<div id="header-wrapper">\
    <div id="header">\
        <a id="logo-wrapper" href="">\
            <img id="logo" alt="X-Wall Logo" src="images/logo.png">\
            <h1 id="title" data-document-title="插墙有道 (X-WALL)">插墙有道</h1>\
            <span id="domain">x-wall.org</span>\
        </a>\
        <ul id="nav">\
            <li><a href="#introduction">简介</a></li>\
            <li><a href="#tutorial">使用教程</a></li>\
            <li><a href="https://github.com/vilic/x-wall" target="_blank">Github</a></li>\
            <li><a href="https://github.com/vilic/x-wall/issues/new" target="_blank">信息反馈</a></li>\
            <li><a href="http://me.alipay.com/vilic" target="_blank">捐赠</a></li>\
        </ul>\
    </div>\
</div>\
<div id="main-wrapper">\
    <div id="body-wrapper">\
        <div id="content-wrapper">\
            <!-- Introduction -->\
            <a id="introduction" class="anchor">&nbsp;</a>\
            <h3>简介</h3>\
            <h4>X-Wall 是什么?</h4>\
            <p>X-Wall 是一个整合了 <a href="http://www.privoxy.org/">Privoxy</a> 和 Plink (Plink 是 <a href="http://www.chiark.greenend.org.uk/%7Esgtatham/putty/" target="_blank">PuTTy</a>的一部分) 的小工具, 提供给大家一个更简单方便的科学上网方案.</p>\
            <h4>系统要求</h4>\
            <p>Microsoft Windows 2000/XP/7/8 (顺便只有 XP 和 7 测试过)<br>\
            Microsoft .NET Framework 3.5 SP1 (Windows 7 上已经预装)</p>\
            <h4>其他要求</h4>\
            <p>你需要自己准备 SSH 账号 (或者 HTTP 代理), 但 SSH 账号通常不是免费的 (也不贵, 便宜的一年不超过50元).</p>\
            <h4>特性</h4>\
            <ul>\
                <li>安装配置傻瓜. 需要做的仅仅是解压文件到一个合适的位置, 运行 X-Wall.exe, 输入 SSH 账号信息, 连接即可.</li>\
                <li>内置 GFWList, 每天检查更新. 访问没有被墙的网站则直接连接.</li>\
                <li>支持自定义规则. 需要添加规则时, 从浏览器复制链接, 右键 X-Wall 的托盘图标, 点击 "添加规则" 后按提示操作即可.</li>\
                <li>可以通过 WiFi 把代理共享给手机 (需要取消 "只监听本地端口").</li>\
            </ul>\
            <!-- Tutorial -->\
            <a id="tutorial" class="anchor">&nbsp;</a>\
            <h3>教程</h3>\
            <p>还在制作中... 很快就好.</p>\
        </div>\
        <div id="sidebar-wrapper">\
            <div class="item dark">\
                <h2>下载 Alpha 测试版</h2>\
                <p>请选择你想下载的文件包.</p>\
                <ul>\
                    <!--<li><a href="#">Windows Installer</a></li>-->\
                    <li><a href="https://raw.github.com/vilic/x-wall/master/x-wall-alpha.rar">RAR 压缩文件</a></li>\
                </ul>\
            </div>\
            <!--\
            <div class="item light">\
                <h2>Downloads</h2>\
                <p>Please select the package you want to download.</p>\
                <ul>\
                    <li><a href="#">Windows Installer</a></li>\
                    <li><a href="#">ZIP File</a></li>\
                </ul>\
            </div>\
            -->\
        </div>\
    </div>\
    <div id="footer">\
        <div>架设在 <a href="https://github.com/" target="_blank">Github</a></div>\
        <div>©2012 <a href="http://vilic.info/" target="_blank">VILIC VANE</a></div>\
    </div>\
</div>');