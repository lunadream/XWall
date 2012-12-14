using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using XWall.Properties;

namespace XWall {
    static class Rules {
        static Settings settings = Settings.Default;
        static CustomRulesEditor editor = new CustomRulesEditor();
        static bool changedInQueue = false;
        static bool generatingActionFile = false;

        static bool initialized = false;

        public static void Initialize() {
            if (initialized) return;
            initialized = true;

            OnlineRules.Updated += (success) => {
                raiseChangedEvent();
            };

            CustomRules.Rules.ListChanged += (sender, e) => {
                raiseChangedEvent();
            };

            Changed += GenerateActionFile;

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "ProxyType": break;
                    case "SshSocksPort": break;
                    case "HttpServer": break;
                    case "HttpPort": break;
                    case "UseOnlineRules": break;
                    case "UseCustomRules": break;
                    default: return;
                }

                GenerateActionFile();
            };

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "UseOnlineRules": break;
                    default: return;
                }

                if (settings.UseOnlineRules && !File.Exists(App.AppDataDirectory + settings.OnlineRulesFileName))
                    OnlineRules.Update();
            };

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "SubmitNewRule": break;
                    default: return;
                }

                updateNewRuleSubmitToggleFile();
            };

            //if (!File.Exists(settings.PrivoxyActionFileName))

            GenerateActionFile();
            updateNewRuleSubmitToggleFile();

            if (
                settings.UseOnlineRules &&
                (DateTime.Now - settings.OnlineRulesLastUpdateTime).Ticks / 10000000 >= settings.OnlineRulesUpdateInterval ||
                !File.Exists(App.AppDataDirectory + settings.OnlineRulesFileName)
            ) OnlineRules.Update();
        }

        static void updateNewRuleSubmitToggleFile() {
            File.WriteAllText(App.AppDataDirectory + settings.PrivoxyTemplatesFolderName + settings.SubmitNewRuleToggleFileName, settings.SubmitNewRule.ToString().ToLower());
        }

        public static void GenerateActionFile() {
            if (generatingActionFile) return;
            generatingActionFile = true;

            new Action(() => {
                string text;
                if (settings.UseOnlineRules && File.Exists(App.AppDataDirectory + settings.OnlineRulesFileName))
                    text = File.ReadAllText(App.AppDataDirectory + settings.OnlineRulesFileName);
                else
                    text = "";

                string forwardSettings;

                if (settings.ProxyType == "SSH")
                    forwardSettings = "forward-socks5 127.0.0.1:" + settings.SshSocksPort + " .";
                else
                    forwardSettings = "forward " + (settings.HttpServer != "" ? settings.HttpServer + ":" : "127.0.0.1:") + settings.HttpPort;
                
                text = text.Replace("$forward-settings$", forwardSettings);

                var defaultProxy = Operation.Proxies.DefaultProxy;

                var defaultForwardSettings = "forward " + (string.IsNullOrEmpty(defaultProxy) ? "." : defaultProxy);

                text = text.Replace("$default-forward-settings$", defaultForwardSettings);

                if (settings.UseCustomRules) {
                    var matches = new Regex(@".+").Matches(settings.CustomRules);
                    
                    var forwardRules = new List<string>();
                    var doNotForwardRules = new List<string>();

                    foreach (Match match in matches) {
                        if (match.Value.StartsWith("!")) {
                            doNotForwardRules.Add(match.Value.Substring(1));
                        }
                        else {
                            forwardRules.Add(match.Value);
                        }
                    }

                    text +=
                        "\r\n\r\n" +
                        "# Custom rules\r\n\r\n" +
                        "{+forward-override{" + forwardSettings + "}}" + "\r\n" +
                        String.Join("\r\n", forwardRules.ToArray()) + "\r\n" +
                        "{+forward-override{" + defaultForwardSettings + "}}" + "\r\n" +
                        String.Join("\r\n", doNotForwardRules.ToArray())+ "\r\n";
                }

                File.WriteAllText(App.AppDataDirectory + settings.PrivoxyActionFileName, text);
                generatingActionFile = false;
            }).BeginInvoke(null, null);
        }

        private static void raiseChangedEvent() {
            if (changedInQueue) return;
            changedInQueue = true;

            new Action(() => {
                Thread.Sleep(100);
                Changed();
                changedInQueue = false;
            }).BeginInvoke(null, null);
        }

        public static void OpenEditor(bool inParentWindow = true) {
            editor.Owner = inParentWindow ? App.Current.MainWindow : null;
            editor.WindowStartupLocation = inParentWindow ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            if (inParentWindow)
                editor.customRulesExpander.IsExpanded = inParentWindow;
            editor.Show();
            editor.Activate();
        }

        public static class OnlineRules {
            static bool updating = false;

            static System.Timers.Timer updateTimer;

            public static void Update() {
                if (updating) return;
                updating = true;

                if (updateTimer != null)
                    updateTimer.Dispose();

                updateTimer = new System.Timers.Timer(settings.OnlineRulesUpdateInterval * 1000);
                updateTimer.Elapsed += (sender, e) => {
                    Update();
                };
                updateTimer.Start();

                UpdateStarted();
                var client = new WebClient();

                var timer = new Timer((arg) => {
                    client.CancelAsync();
                    Updated(false);
                }, null, settings.RuleUpdateTimeout, Timeout.Infinite);

                client.Proxy = null;
                client.DownloadStringCompleted += (sender, e) => {
                    timer.Dispose();
                    if (e.Error != null)
                        Updated(false);
                    else {
                        File.WriteAllText(App.AppDataDirectory + settings.OnlineRulesFileName, e.Result);
                        settings.OnlineRulesLastUpdateTime = DateTime.Now;
                        Updated(true);
                    }
                    updating = false;
                };
                client.DownloadStringAsync(new Uri(settings.OnlineRulesUrl));
            }

            public static event Action UpdateStarted = () => { };

            public delegate void UpdatedHandler(bool success);
            public static event UpdatedHandler Updated = (success) => { };
        }

        public static class CustomRules {
            public static BindingList<string> Rules = initRules();

            static BindingList<string> initRules() {
                List<string> list;
                if (settings.CustomRules != "")
                    list = new List<string>(settings.CustomRules.Split('\n'));
                else
                    list = new List<string>();

                var bingdingList = new BindingList<string>(list);
                bingdingList.ListChanged += (sender, e) => {
                    settings.CustomRules = String.Join("\n", bingdingList.ToArray());
                };
                return bingdingList;
            }

            public static bool Add(string rule) {
                if (Rules.Contains(rule)) {
                    return false;
                }
                else {
                    Rules.Add(rule);
                    return true;
                }
            }

            public static bool Delete(string rule) {
                return Rules.Remove(rule);
            }
        }

        public static event Action Changed = () => { };
    }
}
