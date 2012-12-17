using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XWall.Properties;

namespace XWall {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        static Settings settings = Settings.Default;
        static ResourceManager resourceManager = Properties.Resources.ResourceManager;
        static ResourceDictionary resources = App.Current.Resources;
        NotificationController notificationController;
        Plink plink;
        Privoxy privoxy;

        public MainWindow() {
            if (App.IsShutDown)
                return;

            InitializeComponent();
            InitializeBinding();

            notificationController = new NotificationController(this);
            plink = new Plink();
            privoxy = new Privoxy();
        }

        private void onWindowLoaded(object sender, RoutedEventArgs e) {
            versionTextBlock.Text += resources["Version"] as string + " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            websiteUrlText.Text = settings.WebsiteUrl;
            feedbackEmailText.Text = settings.FeedbackEmail;

            if (
                settings.ProxyType == "SSH" && Plink.CheckSettings() ||
                settings.ProxyType == "HTTP"
            ) WindowState = WindowState.Minimized;

            plink.Started += () => {
                Dispatcher.BeginInvoke(new Action(() => {
                    sshInformationGrid.IsEnabled = false;
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Stop"] as string;
                    notificationController.SetStatus(NotificationController.Status.Processing, resources["Connecting"] as string);
                }));
            };

            plink.Connected += () => {
                Dispatcher.BeginInvoke(new Action(() => {
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Disconnect"] as string;
                    notificationController.SetStatus(NotificationController.Status.OK, resources["Connected"] as string, settings.SshNotification ? String.Format(resources["SuccessConnectDescription"] as string, settings.SshServer) : null);
                }));
            };

            plink.ReconnectCountingDown += (seconds) => {
                Dispatcher.BeginInvoke(new Action(() => {
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Stop"] as string;
                    notificationController.SetStatus(NotificationController.Status.Stopped, String.Format(resources["ReconnectDescription"] as string, seconds));
                }));
            };

            plink.Disconnected += (isLastSuccess, isReconnect) => {
                Dispatcher.BeginInvoke(new Action(() => {
                    sshInformationGrid.IsEnabled = true;
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Connect"] as string;

                    if (plink.Error != null)
                        notificationController.SetStatus(NotificationController.Status.Error, resources["ErrorConnect"] as string, plink.Error, System.Windows.Forms.ToolTipIcon.Error);
                    else if (isLastSuccess)
                        notificationController.SetStatus(NotificationController.Status.Stopped, resources["Disconnected"] as string, settings.SshNotification ? resources["DisconnectedDescription"] as string : null, System.Windows.Forms.ToolTipIcon.Warning);
                    else if (plink.IsNormallyStopped)
                        notificationController.SetStatus(NotificationController.Status.Stopped, resources["ConnectStopped"] as string);
                    else if (isReconnect)
                        notificationController.SetStatus(NotificationController.Status.Stopped, resources["ConnectFailed"] as string);
                    else
                        notificationController.SetStatus(NotificationController.Status.Stopped, resources["ConnectFailed"] as string, String.Format(resources["ConnectFailedDescription"] as string, settings.SshServer), System.Windows.Forms.ToolTipIcon.Warning);
                }));
            };

            //RULES
            //online rules
            if (settings.OnlineRulesLastUpdateTime.Ticks > 0) {
                lastUpdateTimeTextBlock.Text = settings.OnlineRulesLastUpdateTime.ToString(@"M\/d\/yyyy");
            }

            Rules.OnlineRules.UpdateStarted += () => {
                Dispatcher.BeginInvoke(new Action(() => {
                    onlineRulesUpdateButton.Content = resources["Updating"] as string;
                    onlineRulesUpdateButton.IsEnabled = false;
                }));
            };

            Rules.OnlineRules.Updated += (success) => {
                Dispatcher.BeginInvoke(new Action(() => {
                    onlineRulesUpdateButton.Content = resources["Update"] as string;
                    onlineRulesUpdateButton.IsEnabled = true;
                    if (success)
                        lastUpdateTimeTextBlock.Text = settings.OnlineRulesLastUpdateTime.ToString(@"M\/d\/yyyy");
                    else
                        notificationController.Tray.ShowBalloonTip(0, resources["UpdateOnlineRulesFailed"] as string, resources["UpdateOnlineRulesFailedDescription"] as string, System.Windows.Forms.ToolTipIcon.Warning);
                }));
            };

            //custom rules
            updateCustomRulesStatus();

            Rules.CustomRules.Rules.ListChanged += (o, a) => {
                Dispatcher.BeginInvoke(new Action(updateCustomRulesStatus));
            };

            Rules.Initialize();

            privoxy.Start();

            settings.PropertyChanged += (o, a) => {
                switch (a.PropertyName) {
                    case "ProxyPort": break;
                    case "ListenToLocalOnly": break;
                    default: return;
                }

                privoxy.Start();
            };

            if (settings.ProxyType == "SSH" && settings.AutoStart) {
                plink.Start();
            }

            settings.PropertyChanged += (o, a) => {
                switch (a.PropertyName) {
                    case "ProxyType": break;
                    default: return;
                }

                if (settings.ProxyType == "SSH") {
                    if (settings.AutoStart)
                        plink.Start();
                }
                else
                    plink.Stop();
            };

            settings.PropertyChanged += (o, a) => {
                switch (a.PropertyName) {
                    case "SshSocksPort": break;
                    case "SshCompression": break;
                    case "SshPlonkKeyword": break;
                    default: return;
                }

                if (settings.ProxyType == "SSH") {
                    if (plink.IsConnected || plink.IsConnecting)
                        plink.Start();
                }
            };

            var usePlonkChangeBack = false;
            settings.PropertyChanged += (o, a) => {
                switch (a.PropertyName) {
                    case "SshUsePlonk":
                        if (usePlonkChangeBack) {
                            usePlonkChangeBack = false;
                            return;
                        }
                        else break;
                    default: return;
                }

                if (settings.SshUsePlonk && !File.Exists(settings.PlonkFileName)) {
                    usePlonkChangeBack = true;
                    sshUsePlonkCheckBox.IsChecked = false;
                    MessageBox.Show(resources["PlonkMissingMessage"] as string);
                }
                else if (settings.ProxyType == "SSH") {
                    if (plink.IsConnected || plink.IsConnecting)
                        plink.Start();
                }
            };

            var ruleCommandWatcher = new FileSystemWatcher(App.AppDataDirectory, "*-cmd");
            ruleCommandWatcher.Created += ruleCommandHandler;
            ruleCommandWatcher.Changed += ruleCommandHandler;
            ruleCommandWatcher.EnableRaisingEvents = true;

            checkVersion();

            if (WindowState != WindowState.Minimized)
                Activate();

            if (!settings.SubmitNewRuleAsked) {
                settings.SubmitNewRuleAsked = true;
                var message = (resources["ShareRuleMessage"] as string).Replace("%n ", Environment.NewLine + Environment.NewLine);

                var result = MessageBox.Show(message, resources["ShareRuleTitle"] as string, MessageBoxButton.YesNo);
                settings.SubmitNewRule = result == MessageBoxResult.Yes;
            }
        }

        void updateCustomRulesStatus() {
            var count = Rules.CustomRules.Rules.Count;

            string text;
            if (count == 0)
                text = resources["NoCustomRuleDescription"] as string;
            else if (count == 1)
                text = resources["OneCustomRuleDescription"] as string;
            else
                text = String.Format(resources["CustomRulesDescription"] as string, count);

            customRulesCountTextBlock.Text = text;
        }

        private void onWebsiteHyperLinkClick(object sender, RoutedEventArgs e) {
            Process.Start(settings.WebsiteUrl);
        }

        private void onFeedbackEmailClick(object sender, RoutedEventArgs e) {
            Process.Start("mailto:" + settings.FeedbackEmail);
        }

        private void onShowUrlInfoHyperlinkClick(object sender, RoutedEventArgs e) {
            Process.Start(settings.ShowUrlInfoUrl);
        }

        private void onWindowClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            hideButton.Focus();
            WindowState = WindowState.Minimized;
        }

        private void onWindowStateChanged(object sender, EventArgs e) {
            switch (WindowState) {
                case WindowState.Minimized:
                    ShowInTaskbar = false;
                    break;
                case WindowState.Normal:
                    ShowInTaskbar = true;
                    break;
            }
        }

        private void onCustomRulesEditButtonClick(object sender, RoutedEventArgs e) {
            Rules.OpenEditor();
        }

        private void onExitButtonClick(object sender, RoutedEventArgs e) {
            App.Current.Shutdown();
        }

        private void onHideButtonClick(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        private void onOnlineRulesUpdateButtonClick(object sender, RoutedEventArgs e) {
            Rules.OnlineRules.Update();
        }

        private void onSshConnectButtonClick(object sender, RoutedEventArgs e) {
            if (plink.IsConnected || plink.IsConnecting)
                plink.Stop();
            else if (plink.IsReconnecting) {
                sshConnectButton.IsEnabled = false;
                plink.StopReconnect = true;
            }
            else {
                if (
                    checkSetting(settings.SshServer.Trim() != "", sshServerTextBox, resources["EmptySshServerMessage"] as string) &&
                    checkSetting(settings.SshPort > 0, sshPortTextBox, resources["InvalidSshPortMessage"] as string) &&
                    checkSetting(settings.SshUsername.Trim() != "", sshUsernameTextBox, resources["EmptySshUsernameMessage"] as string) &&
                    checkSetting(settings.SshPassword.Trim() != "", sshPasswordBox, resources["EmptySshPasswordMessage"] as string) &&
                    checkSetting(settings.SshSocksPort > 0, sshSocksPortTextBox, resources["InvalidSocksPortMessage"] as string, advancedSettingsTabItem)
                )
                plink.Start();
            }
        }

        bool checkSetting(bool result, TextBox textBox, string message, TabItem tab = null) {
            if (!result) {
                MessageBox.Show(message, resources["Connect"] as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                if (tab != null) tab.Focus();
                new Action(() => {
                    Dispatcher.BeginInvoke(new Action(() => {
                        textBox.SelectAll();
                        textBox.Focus();
                    }));
                }).BeginInvoke(null, null);
            }

            return result;
        }

        bool checkSetting(bool result, PasswordBox passwordBox, string message) {
            if (!result) {
                MessageBox.Show(message, resources["Connect"] as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }

            return result;
        }

        string onlineVersionStr = null;
        bool updateDownloaded = false;

        void checkVersion() {
            downloadUpdateButton.IsEnabled = false;
            onlineVersionTextBlock.Text = resources["Checking"] as string;

            var url = new Uri(settings.OnlineVersionUrl);

            var client = new WebClient();
            client.Proxy = null;
            client.DownloadStringAsync(url);

            client.DownloadStringCompleted += (sender, e) => {
                Dispatcher.BeginInvoke(new Action(() => {
                    if (e.Error == null) {
                        bool suggestedToUpdate = false;

                        var versions = e.Result.Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        var installedVersion = Assembly.GetExecutingAssembly().GetName().Version;
                        onlineVersionStr = versions[0];
                        var onlineVersion = new Version(onlineVersionStr);
                        var lowVersion = new Version(versions[1]);

                        if (File.Exists(App.AppDataDirectory + settings.UpdateMarkName)) {
                            File.Delete(App.AppDataDirectory + settings.UpdateMarkName);
                            if (installedVersion < onlineVersion)
                                notificationController.SendMessage(resources["UpdateFailedTitle"] as string, resources["UpdateFailedDetails"] as string, System.Windows.Forms.ToolTipIcon.Error);
                            else
                                notificationController.SendMessage(resources["UpdateSuccessTitle"] as string, resources["UpdateSuccessDetails"] as string);
                        }

                        if (installedVersion < lowVersion) {
                            suggestedToUpdate = settings.DismissedUpdateVersion != onlineVersionStr;

                            if (onlineVersionStr != settings.DismissedUpdateVersion) {
                                suggestedToUpdate = true;
                                settings.DismissedUpdateVersion = onlineVersionStr;
                            }
                        }

                        onlineVersionTextBlock.Text = resources["Version"] as string + " " + onlineVersionStr;
                        downloadUpdateButton.IsEnabled = true;

                        if (suggestedToUpdate) {
                            var result = MessageBox.Show(String.Format(resources["UpdateAvailableDescription"] as string, onlineVersionStr), resources["XWallTitle"] as string, MessageBoxButton.OKCancel);
                            if (result == MessageBoxResult.OK)
                                downloadUpdate();
                        }
                    }
                    else
                        onlineVersionTextBlock.Text = resources["OnlineVersionCheckFailed"] as string;
                }));
            };
        }

        void downloadUpdate() {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (version == onlineVersionStr) {
                var result = MessageBox.Show(resources["SameVersionMessage"] as string, resources["XWallTitle"] as string, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;
            }

            if (updateDownloaded)
                startUpdateInstalling();
            else {
                downloadUpdateButton.IsEnabled = false;

                var url = new Uri(settings.UpdateInstallerUrl);

                var client = new WebClient();
                client.Proxy = null;
                client.DownloadFileAsync(url, App.AppDataDirectory + settings.UpdateInstallerName);

                client.DownloadProgressChanged += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(() => {
                        onlineVersionTextBlock.Text = String.Format(resources["Downloading"] as string, e.ProgressPercentage);
                    }));
                };

                client.DownloadFileCompleted += (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(() => {
                        downloadUpdateButton.IsEnabled = true;
                        onlineVersionTextBlock.Text = resources["Version"] as string + " " + onlineVersionStr;
                        if (e.Error == null) {
                            updateDownloaded = true;
                            startUpdateInstalling();
                        }
                        else
                            MessageBox.Show(resources["DownloadUpdateFailed"] as string);
                    }));
                };

                onlineVersionTextBlock.Text = String.Format(resources["Downloading"] as string, 0);
            }
        }

        void startUpdateInstalling() {
            var result = MessageBox.Show(resources["InstallUpdateDescription"] as string, resources["XWallTitle"] as string, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK) {
                File.WriteAllText(App.AppDataDirectory + settings.UpdateMarkName, "");
                Process.Start(App.AppDataDirectory + settings.UpdateInstallerName, "/silent");
                App.Current.Shutdown();
            }
        }

        void ruleCommandHandler(object sender, FileSystemEventArgs e) {
            Dispatcher.BeginInvoke(new Action(() => {
                Thread.Sleep(100);
                if (!File.Exists(e.FullPath)) return;
                if (e.Name == settings.NewRuleFileName) {
                    var rule = File.ReadAllText(e.FullPath);
                    Rules.CustomRules.Add(rule);
                    File.Delete(e.FullPath);
                }
                else if (e.Name == settings.DeleteRuleFileName) {
                    var rule = File.ReadAllText(e.FullPath);
                    Rules.CustomRules.Delete(rule);
                    File.Delete(e.FullPath);
                }
            }));
        }

        private void onDownloadUpdateButtonClick(object sender, RoutedEventArgs e) {
            downloadUpdate();
        }

        private void onAboutTabItemGotFocus(object sender, RoutedEventArgs e) {
            if (onlineVersionStr == null)
                checkVersion();
        }
    }
}
