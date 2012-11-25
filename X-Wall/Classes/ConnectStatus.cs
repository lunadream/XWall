using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace XWall {
    partial class MainWindow {
        class ConnectStatus {
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

            public ConnectStatus(MainWindow window) {
                this.window = window;
                icon = new NotifyIcon();
                icon.Visible = true;

                icon.DoubleClick += (sender, e) => {
                    window.WindowState = System.Windows.WindowState.Normal;
                    window.Activate();
                };

                var menu = new ContextMenu();

                menu.MenuItems.Add(resources["AddRules"] as string, (sender, e) => {
                    Rules.OpenEditor(false);
                });

                menu.MenuItems.Add(resources["Exit"] as string, (sender, e) => {
                    System.Windows.Application.Current.Shutdown();
                });

                icon.ContextMenu = menu;

                SetStatus(Status.Stopped, resources["NotConnected"] as string);
                System.Windows.Application.Current.Exit += (sender, e) => {
                    icon.Dispose();
                };
            }

            public void SetStatus(Status status, string message, string tip = null, ToolTipIcon tipIcon = ToolTipIcon.Info) {
                window.sshStatusTextBlock.Text = message;

                icon.Icon = statusIcons[(int)status];
                icon.Text = settings.ProxyType == "SSH" ? resources["XWall"] as string + " - " + message : resources["XWall"] as string;

                if (tip != null)
                    icon.ShowBalloonTip(0, message, tip, tipIcon);
            }
        }
    }
}
