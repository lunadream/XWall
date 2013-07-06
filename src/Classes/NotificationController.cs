using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace XWall {
    partial class MainWindow {
        class NotificationController {
            NotifyIcon icon;
            MainWindow window;
            public enum Status { Stopped = 0, Processing = 1, OK = 2, Error = 3 }
            public NotifyIcon Tray { get { return icon; } }

            static Icon[] statusIcons = new Icon[] {
                resourceManager.GetObject("TrayStoppedIcon") as Icon,
                resourceManager.GetObject("TrayProcessingIcon") as Icon,
                resourceManager.GetObject("TrayOKIcon") as Icon,
                resourceManager.GetObject("TrayErrorIcon") as Icon
            };

            public NotificationController(MainWindow window) {
                this.window = window;
                icon = new NotifyIcon();
                icon.Visible = true;

                icon.DoubleClick += (sender, e) => {
                    window.WindowState = System.Windows.WindowState.Normal;
                    window.Activate();
                };

                icon.BalloonTipClicked += (sender, e) => {
                    window.WindowState = System.Windows.WindowState.Normal;
                    window.Activate();
                };

                var menu = new ContextMenu();

                menu.MenuItems.Add(resources["AddRules"] as string, (sender, e) => {
                    Rules.OpenEditor(false);
                });

                menu.MenuItems.Add(window.profileContextMenu);

                var proxyModeNormalItem = new MenuItem(resources["Normal"] as string);
                var proxyModeForwardAllItem=new MenuItem(resources["ForwardAll"] as string);

                var proxyModeCheckSwitcher = new Action(() => {
                    proxyModeNormalItem.Checked = !settings.ForwardAll;
                    proxyModeForwardAllItem.Checked = settings.ForwardAll;
                });

                proxyModeCheckSwitcher();

                settings.PropertyChanged += (sender, e) => {
                    if (e.PropertyName == "ForwardAll") {
                        proxyModeCheckSwitcher();
                    }
                };

                proxyModeNormalItem.Click += (sender, e) => {
                    if (settings.ForwardAll) {
                        settings.ForwardAll = false;
                    }
                };

                proxyModeForwardAllItem.Click += (sender, e) => {
                    if (!settings.ForwardAll) {
                        settings.ForwardAll = true;
                    }
                };

                proxyModeNormalItem.RadioCheck = true;
                proxyModeForwardAllItem.RadioCheck = true;

                var proxyModeMenuItem = new MenuItem(resources["ProxyMode"] as string, new MenuItem[]{ proxyModeNormalItem, proxyModeForwardAllItem });

                menu.MenuItems.Add(proxyModeMenuItem);

                menu.MenuItems.Add(resources["Exit"] as string, (sender, e) => {
                    System.Windows.Application.Current.Shutdown();
                });

                icon.ContextMenu = menu;

                SetStatus(settings.ProxyType, Status.Stopped, resources["NotConnected"] as string);
                System.Windows.Application.Current.Exit += (sender, e) => {
                    icon.Dispose();
                };
            }

            public void SendMessage(string title, string details, ToolTipIcon tipIcon = ToolTipIcon.Info) {
                icon.ShowBalloonTip(0, title, details, tipIcon);
            }

            public void SetStatus(string type, Status status, string message = null, string tip = null, ToolTipIcon tipIcon = ToolTipIcon.Info) {
                try {
                    if (message != null) {
                        window.sshStatusTextBlock.Text = message;
                    }

                    if (type == settings.ProxyType) {
                        icon.Icon = statusIcons[(int)status];
                        icon.Text = settings.ProxyType == "SSH" && message != null ? resources["XWall"] as string + " - " + message : resources["XWall"] as string;
                    }

                    if (tip != null)
                        icon.ShowBalloonTip(0, message, tip, tipIcon);
                }
                catch { }
            }
        }
    }
}
