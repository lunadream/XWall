using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using XWall.Properties;

namespace XWall {
    partial class MainWindow {
        void InitializeBinding() {
            //BASIC SETTINGS
            //proxy type
            bindProxyType();

            //ssh information
            UIBinding.bindTextBox(sshServerTextBox, "SshServer");
            UIBinding.bindTextBox(sshPortTextBox, "SshPort");
            UIBinding.bindTextBox(sshUsernameTextBox, "SshUsername");
            UIBinding.bindTextBox(sshPasswordBox, "SshPassword");

            //http information
            UIBinding.bindTextBox(httpServerTextBox, "HttpServer");
            UIBinding.bindTextBox(httpPortTextBox, "HttpPort");

            //ADVANCED SETTINGS
            //x-wall
            UIBinding.bindCheckBox(autoStartCheckBox, "AutoStart");
            UIBinding.bindCheckBox(listenToLocalOnlyCheckBox, "ListenToLocalOnly");
            UIBinding.bindTextBox(proxyPortTextBox, "ProxyPort");

            //ssh
            UIBinding.bindCheckBox(sshNotificationCheckBox, "SshNotification");
            UIBinding.bindCheckBox(sshCompressionCheckBox, "SshCompression");
            UIBinding.bindCheckBox(sshAutoReconnectCheckBox, "SshAutoReconnect");
            UIBinding.bindTextBox(sshSocksPortTextBox, "SshSocksPort");

            //RULES
            UIBinding.bindCheckBox(useOnlineRulesCheckBox, "UseOnlineRules");
            UIBinding.bindCheckBox(submitNewRuleCheckBox, "SubmitNewRule");
            //UIBinding.bindCheckBox(useCustomRulesCheckBox, "UseCustomRules");
        }

        void bindProxyType() {
            //settings to control
            switch (settings.ProxyType) {
                case "SSH":
                    proxyTypeSshRadio.IsChecked = true;
                    break;
                case "HTTP":
                    proxyTypeHttpRadio.IsChecked = true;
                    break;
            }

            //control to settings
            proxyTypeSshRadio.Checked += (sender, e) => {
                settings.ProxyType = "SSH";

            };

            proxyTypeHttpRadio.Checked += (sender, e) => {
                settings.ProxyType = "HTTP";
            };
        }
    }

    partial class CustomRulesEditor {
        void InitializeBinding() {
            UIBinding.bindCheckBox(addSubdomainsCheckBox, "CustomRulesAddSubdomains");
        }
    }

    static class UIBinding {
        static Settings settings = Settings.Default;

        public static void bindTextBox(TextBox textBox, string name) {
            var binding = new Binding(name);
            binding.Source = settings;
            textBox.SetBinding(TextBox.TextProperty, binding);
            textBox.GotFocus += (sender, e) => {
                textBox.SelectAll();
            };
        }

        public static void bindTextBox(PasswordBox passwordBox, string name) {
            passwordBox.Password = settings[name] as string;
            passwordBox.PasswordChanged += (sender, e) => {
                settings[name] = passwordBox.Password;
            };
            passwordBox.GotFocus += (sender, e) => {
                passwordBox.SelectAll();
            };
        }

        public static void bindCheckBox(CheckBox checkBox, string name) {
            var binding = new Binding(name);
            binding.Source = settings;
            checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
        }

    }
}
