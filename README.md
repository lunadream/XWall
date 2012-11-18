[English](#x-wall-english) | [简体中文](#x-wall-chinese)

<a id="x-wall-english"></a>
# Introduction to X-Wall

## What is X-Wall?

X-Wall is a small tool wrapped [Privoxy](http://www.privoxy.org/) and Plink (which is part of [PuTTy](http://www.chiark.greenend.org.uk/~sgtatham/putty/)) together to provide people an easier way to X (cross) the wall.

## System Requirements

Microsoft Windows 2000/XP/7/8 (By the way only XP and 7 are tested)  
Microsoft .NET Framework 3.5 SP1 (pre-installed on Windows 7)

## Other Requirements

You will need to prepare the SSH account (or a HTTP proxy) yourself, which is usually not free (but also not expensive, many within 50 RMB per year).

## Features

- Easy to setup and configurate. What you need to do is just extract the files to a proper place, start X-Wall.exe, enter your SSH account information and connect.
- Built-in GFWList and will check it online everyday, when you visit websites that are not blocked, it will connect directly.
- Support custom rules. Just copy the link you want to add from your browser, right click the notification icon of X-Wall, and then "Add rules".
- Ability to share the proxy with your phone via WiFi (need to cancel "Listen to local address only").

## Download

I am now using this version myself, and I think it is usable. Feedback is always welcome, my email: [i@vilic.info](mailto:i@vilic.info).

Downloads:

> [Alpha Version](https://raw.github.com/vilic/x-wall/master/x-wall-alpha.rar)

# About GFWList2Privoxy

You can also use this list for your own Privoxy, the generator is written in JavaScript, and you will be able to execute it on your Windows directly.

Downloads:

> [Action File](https://raw.github.com/vilic/x-wall/master/rules/gfwlist.action)  
> [Generator](https://raw.github.com/vilic/x-wall/master/rules/gfwlist2privoxy.js)

<a id="x-wall-chinese"></a>
# X-Wall 简介

## X-Wall 是什么?

X-Wall 是一个整合了 [Privoxy](http://www.privoxy.org/) 和 Plink (Plink 是 [PuTTy](http://www.chiark.greenend.org.uk/~sgtatham/putty/) 的一部分) 的小工具, 提供给大家一个更简单方便的科学上网方案.

## 系统要求

Microsoft Windows 2000/XP/7/8 (顺便只有 XP 和 7 测试过)  
Microsoft .NET Framework 3.5 SP1 (Windows 7 上已经预装)

## 其他要求

你需要自己准备 SSH 账号 (或者 HTTP 代理), 但 SSH 账号通常不是免费的 (也不贵, 便宜的一年不超过50元).

## 特性

- 安装配置傻瓜. 需要做的仅仅是解压文件到一个合适的位置, 运行 X-Wall.exe, 输入 SSH 账号信息, 连接即可.
- 内置 GFWList, 每天检查更新. 访问没有被墙的网站则直接连接.
- 支持自定义规则. 需要添加规则时, 从浏览器复制链接, 右键 X-Wall 的托盘图标, 点击 "添加规则" 后按提示操作即可.
- 可以通过 WiFi 把代理共享给手机 (需要取消 "只监听本地端口").

## 下载

我现在用的就是这个版本, 基本没什么问题. 欢迎发送反馈信息到我的邮箱: [i@vilic.info](mailto:i@vilic.info).

下载链接:

> [小范围测试版本](https://raw.github.com/vilic/x-wall/master/x-wall-alpha.rar)

# 关于 GFWList2Privoxy

也可以把这个列表用在自己的 Privoxy 上. 生成器是 JavaScript 写的, 可以直接在 Windows 上双击运行.

下载链接:

> [Action 文件](https://raw.github.com/vilic/x-wall/master/rules/gfwlist.action)  
> [生成器](https://raw.github.com/vilic/x-wall/master/rules/gfwlist2privoxy.js)