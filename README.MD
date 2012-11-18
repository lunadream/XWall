
# Introduction to X-Wall

## What is X-Wall?

X-Wall is a small tool wrapped Privoxy and Plink together to provide people an easier way to X(cross) the wall.

## System Requirements

Microsoft Windows 2000/XP/7/8 (By the way only XP and 7 are tested)
Microsoft .NET Framework 3.5 SP1 (pre-installed on Windows 7)

## Other Requirements

You will need to prepare the SSH account (or a HTTP proxy) yourself, which is usually not free (but also not expensive, many within 50 RMB per year).

## Features

- Easy to setup and configurate. What you need to do is just extract the files to a proper place, start X-Wall.exe, enter your SSH account information and connect.
- Built-in GFWList and will check it online everyday, when you visit websites that are not blocked, it will connect directly.
- Support custom rules. Just copy the link you want to add from your browser, right click the notification icon of X-Wall, and then "Add rules".
- Ability to share the proxy with your phone via WiFi.

## Download

I am now using this version myself, and I think it is usable. Feedback is always welcome, my email: [i@vilic.info](mailto:i@vilic.info).

[Alpha Version](https://raw.github.com/vilic/x-wall/master/x-wall-alpha.rar)


# About GFWList2Privoxy

You can also use this list for your own Privoxy, the generator is written in JavaScript, and you will be able to execute it on your Windows directly.

Downloads:
[Action File](https://raw.github.com/vilic/x-wall/master/rules/gfwlist.action)
[Generator](https://raw.github.com/vilic/x-wall/master/rules/gfwlist2privoxy.js)