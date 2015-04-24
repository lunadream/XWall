using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XWall.Properties;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

namespace XWall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// TODO:Remove '//' in line 409 before release build
    /// </summary>
    public partial class MainWindow : Window
    {
        static Settings settings = Settings.Default;
        static ResourceManager resourceManager = Properties.Resources.ResourceManager;
        static ResourceDictionary resources = App.Current.Resources;
        NotificationController notificationController;
        Plink plink;
        Privoxy privoxy;
        GoAgent goagent;

        Profile.SshProfilesCollection sshProfiles;

        public MainWindow()
        {
            if (App.IsShutDown)
                return;

            InitializeComponent();
        }

        private void onWindowLoaded(object sender, RoutedEventArgs e)
        {
            InitializeBinding();
            AppSetting.MoveOldConfig();
            var gaAppIdsToolTip = new ToolTip();
            gaAppIdsToolTip.Content = resources["GaAppIdsTooltip"] as string;
            gaAppIdsToolTip.StaysOpen = true;
            gaAppIdsToolTip.PlacementTarget = gaAppIdsTextBox;
            gaAppIdsToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
            gaAppIdsTextBox.ToolTip = gaAppIdsToolTip;

            if (settings.SshUsePrivateKeyLogin == true)
            {
                checkbox_sshusekey.IsChecked = true;
                button_sshkeyselect.Visibility = System.Windows.Visibility.Visible;
                label_sshpwd.Content = label_sshpwd.Content = resources["SshTypeKeyText"]; 
            }
            else
            {
                checkbox_sshusekey.IsChecked = false;
                button_sshkeyselect.Visibility = System.Windows.Visibility.Collapsed;
                label_sshpwd.Content = label_sshpwd.Content = resources["Password"]; 

                
            }
            notificationController = new NotificationController(this);
            plink = new Plink();
            privoxy = new Privoxy();
            goagent = new GoAgent();


            versionTextBlock.Text += resources["Version"] as string + " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            websiteUrlText.Text = settings.WebsiteUrl;
            //feedbackEmailText.Text = settings.FeedbackEmail;

            if (!(
                (settings.ProxyType == "SSH" && !Plink.CheckSettings()) ||
                (settings.ProxyType == "GA" && settings.GaAppIds == "")
            )) WindowState = WindowState.Minimized;

            goagent.Started += () =>
            {
                notificationController.SetStatus("GA", NotificationController.Status.OK);
            };

            goagent.Stopped += () =>
            {
                notificationController.SetStatus("GA", NotificationController.Status.Stopped);
            };

            //goagent.RequireRunas += () => {
            //    MessageBox.Show(resources["GoAgentRequiresRunasMessage"] as string);

            //    //var si = new ProcessStartInfo();
            //    //si.FileName = System.Windows.Forms.Application.ExecutablePath;
            //    //si.Verb = "runas";
            //    //si.Arguments = "restart";
            //    //Process.Start(si);
            //    Process.Start(System.Windows.Forms.Application.ExecutablePath, "restart");


            //    //App.Current.Shutdown();
            //};


            plink.Started += () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    sshInformationGrid.IsEnabled = false;
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Stop"] as string;
                    notificationController.SetStatus("SSH", NotificationController.Status.Processing, resources["Connecting"] as string);
                }));
            };

            plink.Connected += () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Disconnect"] as string;
                    notificationController.SetStatus("SSH", NotificationController.Status.OK, resources["Connected"] as string, settings.SshNotification ? String.Format(resources["SuccessConnectDescription"] as string, settings.SshServer) : null);
                }));
            };

            plink.ReconnectCountingDown += (seconds) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Stop"] as string;
                    notificationController.SetStatus("SSH", NotificationController.Status.Stopped, String.Format(resources["ReconnectDescription"] as string, seconds));
                }));
            };

            plink.Disconnected += (isLastSuccess, isReconnect) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    sshInformationGrid.IsEnabled = true;
                    sshConnectButton.IsEnabled = true;
                    sshConnectButton.Content = resources["Connect"] as string;

                    if (plink.Error != null)
                        notificationController.SetStatus("SSH", NotificationController.Status.Error, resources["ErrorConnect"] as string, plink.Error != lastPlinkError ? plink.Error : null, System.Windows.Forms.ToolTipIcon.Error);
                    else if (isLastSuccess)
                        notificationController.SetStatus("SSH", NotificationController.Status.Stopped, resources["Disconnected"] as string, settings.SshNotification ? resources["DisconnectedDescription"] as string : null, System.Windows.Forms.ToolTipIcon.Warning);
                    else if (plink.IsNormallyStopped)
                        notificationController.SetStatus("SSH", NotificationController.Status.Stopped, resources["ConnectStopped"] as string);
                    else if (isReconnect)
                        notificationController.SetStatus("SSH", NotificationController.Status.Stopped, resources["ConnectFailed"] as string);
                    else
                        notificationController.SetStatus("SSH", NotificationController.Status.Stopped, resources["ConnectFailed"] as string, String.Format(resources["ConnectFailedDescription"] as string, settings.SshServer), System.Windows.Forms.ToolTipIcon.Warning);

                    lastPlinkError = plink.Error;
                }));
            };

            //RULES
            //online rules
            if (settings.OnlineRulesLastUpdateTime.Ticks > 0)
            {
                lastUpdateTimeTextBlock.Text = settings.OnlineRulesLastUpdateTime.ToString(@"M\/d\/yyyy");
            }

            Rules.OnlineRules.UpdateStarted += () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    onlineRulesUpdateButton.Content = resources["Updating"] as string;
                    onlineRulesUpdateButton.IsEnabled = false;
                }));
            };

            Rules.OnlineRules.Updated += (success) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
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

            Rules.CustomRules.Rules.ListChanged += (o, a) =>
            {
                Dispatcher.BeginInvoke(new Action(updateCustomRulesStatus));
            };

            Rules.Initialize();

            //* DEBUG CODE
            privoxy.Start();

            var server = new Microsoft.VisualStudio.WebHost.Server(settings.LocalServerPort, "/", App.AppDataDirectory + settings.LocalServerFolderName);
            try
            {
                server.Start();
            }
            catch
            {
                MessageBox.Show(string.Format(resources["FailedStartLocalServer"] as string, settings.LocalServerPort));
            }

            App.Current.Exit += (o, a) =>
            {
                try
                {
                    server.Stop();
                }
                catch { }
            };

            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "ProxyPort": break;
                    case "ListenToLocalOnly": break;
                    case "UseIntranetProxy": break;
                    case "IntranetProxyServer": break;
                    case "IntranetProxyPort": break;
                    default: return;
                }

                privoxy.Start();
            };
            //*/

            //ga

            if (settings.ProxyType == "GA")
            {
                goagent.Start();
            }

            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "ProxyType": break;
                    default: return;
                }

                if (settings.ProxyType == "GA")
                {
                    goagent.Start();
                }
                else
                {
                    goagent.Stop();
                }
            };

            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "GaPort": break;
                    case "GaProfile": break;
                    case "GaAppIds": break;
                    default: return;
                }

                if (settings.ProxyType == "GA")
                {
                    goagent.Start();
                }
            };


            //ssh

            if (settings.ProxyType == "SSH" && settings.AutoStart)
            {
                plink.Start();
            }

            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "ProxyType": break;
                    default: return;
                }

                if (settings.ProxyType == "SSH")
                {
                    if (settings.AutoStart)
                        plink.Start();
                }
                else
                    plink.Stop();
            };

            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "SshSocksPort": break;
                    case "SshCompression": break;
                    case "SshPlonkKeyword": break;
                    default: return;
                }

                if (settings.ProxyType == "SSH")
                {
                    if (plink.IsConnected || plink.IsConnecting)
                        plink.Start();
                }
            };

            var usePlonkChangeBack = false;
            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "SshUsePlonk":
                        if (usePlonkChangeBack)
                        {
                            usePlonkChangeBack = false;
                            return;
                        }
                        else break;
                    default: return;
                }

                if (settings.SshUsePlonk && !File.Exists(settings.PlonkFileName))
                {
                    usePlonkChangeBack = true;
                    sshUsePlonkCheckBox.IsChecked = false;
                    MessageBox.Show(resources["PlonkMissingMessage"] as string);
                }
                else if (settings.ProxyType == "SSH")
                {
                    if (plink.IsConnected || plink.IsConnecting)
                        plink.Start();
                }
            };

            //http&socks
            settings.PropertyChanged += (o, a) =>
            {
                switch (a.PropertyName)
                {
                    case "ProxyType": break;
                    default: return;
                }

                initIconStatus();
            };

            initIconStatus();

            var ruleCommandWatcher = new FileSystemWatcher(App.AppDataDirectory + settings.ConfigsFolderName, "*-cmd");
            ruleCommandWatcher.Created += ruleCommandHandler;
            ruleCommandWatcher.Changed += ruleCommandHandler;
            ruleCommandWatcher.EnableRaisingEvents = true;

            Timer updateCheckTimer = null;
            updateCheckTimer = new Timer((st) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (onlineVersionStr == null)
                    {
                        checkVersion();
                    }
                    else
                    {
                        updateCheckTimer.Dispose();
                    }
                }));
            }, null, 0, settings.UpdateCheckDelay * 1000);

            if (App.Updated)
            {
                new Action(() =>
                {
                    Thread.Sleep(1000);
                    try
                    {
                        File.Delete(App.AppDataDirectory + settings.ResourcesFolderName + settings.UpdateInstallerName);
                    }
                    catch { }
                    notificationController.SendMessage(resources["UpdateSuccessTitle"] as string, resources["UpdateSuccessDetails"] as string);

                    if (Directory.Exists(App.AppDataDirectory + settings.GaFolderName) && settings.GaServerVersion > settings.GaLastServerVersion)
                    {
                        settings.GaLastServerVersion = settings.GaServerVersion;
                        //MessageBox.Show(resources["NewGaServerMessage"] as string);
                    }

                }).BeginInvoke(null, null);
            }

            if (App.Updated || App.FirstRun)
            {
                //new WebClient().DownloadStringAsync(new Uri(settings.UpdateReportUrl + "?v=" + Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            }

            if (WindowState != WindowState.Minimized)
                Activate();

            //if (!settings.SubmitNewRuleAsked) {
            //    settings.SubmitNewRuleAsked = true;
            //    var message = (resources["ShareRuleMessage"] as string).Replace("%n ", Environment.NewLine + Environment.NewLine);

            //    var result = MessageBox.Show(message, resources["ShareRuleTitle"] as string, MessageBoxButton.YesNo);
            //    settings.SubmitNewRule = result == MessageBoxResult.Yes;
            //}
        }

        void initIconStatus()
        {
            if (settings.ProxyType == "HTTP" || settings.ProxyType == "SOCKS5")
            {
                notificationController.SetStatus(settings.ProxyType, NotificationController.Status.OK);
            }
            else
            {
                notificationController.SetStatus(settings.ProxyType, NotificationController.Status.Stopped);
            }
        }

        void updateCustomRulesStatus()
        {
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

        private void onWebsiteHyperLinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start(settings.WebsiteUrl);
        }

        private void onFeedbackEmailClick(object sender, RoutedEventArgs e)
        {
            Process.Start("mailto:" + settings.FeedbackEmail);
        }

        private void onDonateLinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start(settings.DonateUrl);
        }

        private void onShowUrlInfoHyperlinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start(settings.ShowUrlInfoUrl);
        }

        private void onWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            hideButton.Focus();
            WindowState = WindowState.Minimized;
        }

        private void onWindowStateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Minimized:
                    ShowInTaskbar = false;
                    break;
                case WindowState.Normal:
                    ShowInTaskbar = true;
                    break;
            }
        }

        private void onCustomRulesEditButtonClick(object sender, RoutedEventArgs e)
        {
            Rules.OpenEditor();
        }

        private void onExitButtonClick(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void onHideButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void onOnlineRulesUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            Rules.OnlineRules.Update();
        }

        string lastPlinkError = null;

        private void onSshConnectButtonClick(object sender, RoutedEventArgs e)
        {
            if (plink.IsConnected || plink.IsConnecting || plink.IsReconnecting)
            {
                plink.Stop();
            }
            else
            {
                if (
                    checkSetting(settings.SshServer.Trim() != "", sshServerTextBox, resources["EmptySshServerMessage"] as string) &&
                    checkSetting(settings.SshPort > 0, sshPortTextBox, resources["InvalidSshPortMessage"] as string) &&
                    checkSetting(settings.SshUsername.Trim() != "", sshUsernameTextBox, resources["EmptySshUsernameMessage"] as string) &&
                    checkSetting(settings.SshPassword.Trim() != "", sshPasswordBox, resources["EmptySshPasswordMessage"] as string) &&
                    checkSetting(settings.SshSocksPort > 0, sshSocksPortTextBox, resources["InvalidSocksPortMessage"] as string, advancedSettingsTabItem)
                )
                {
                    lastPlinkError = null;
                    plink.Start();
                }
            }
        }

        bool checkSetting(bool result, TextBox textBox, string message, TabItem tab = null)
        {
            if (!result)
            {
                MessageBox.Show(message, resources["Connect"] as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                if (tab != null) tab.Focus();
                new Action(() =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        textBox.SelectAll();
                        textBox.Focus();
                    }));
                }).BeginInvoke(null, null);
            }

            return result;
        }

        bool checkSetting(bool result, PasswordBox passwordBox, string message)
        {
            if (!result)
            {
                MessageBox.Show(message, resources["Connect"] as string, MessageBoxButton.OK, MessageBoxImage.Warning);
                passwordBox.SelectAll();
                passwordBox.Focus();
            }

            return result;
        }

        string onlineVersionStr = null;
        bool updateDownloaded = false;
        bool checkingVersion = false;
        bool downloadingUpdate = false;

        void checkVersion()
        {
            if (checkingVersion || downloadingUpdate) return;
            checkingVersion = true;
            downloadUpdateButton.IsEnabled = false;
            onlineVersionTextBlock.Text = resources["Checking"] as string;

            var url = new Uri(settings.OnlineVersionUrl);
            XmlDocument xmlDoc = new XmlDocument();
            var client = new WebClient();
            //client.Proxy = null;
            client.DownloadStringAsync(url);
            client.Encoding = System.Text.Encoding.UTF8;
            client.DownloadStringCompleted += (sender, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    checkingVersion = false;
                    if (e.Error == null)
                    {
                        xmlDoc.LoadXml(e.Result);
                        MainSetting.minVersion = xmlDoc.SelectSingleNode("XWall.UpdateXML/MinVersion").InnerText;
                        MainSetting.latestVersion = xmlDoc.SelectSingleNode("XWall.UpdateXML/LatestVersion").InnerText;
                        //MainSetting.forceUpdate = Convert.ToBoolean(xmlDoc.SelectSingleNode("XWall.UpdateXML/ForceUpdate").InnerText);
                        MainSetting.downloadUrl = xmlDoc.SelectSingleNode("XWall.UpdateXML/DownloadUrl").InnerText;
                        MainSetting.downloadUrlFull = xmlDoc.SelectSingleNode("XWall.UpdateXML/DownloadUrlFull").InnerText;
                        MainSetting.updateLog = xmlDoc.SelectSingleNode("XWall.UpdateXML/UpdateLog").InnerText.Replace("\\newline", Environment.NewLine);
                        bool suggestedToUpdate = false;
                        var installedVersion = Assembly.GetExecutingAssembly().GetName().Version;
                        onlineVersionStr = MainSetting.latestVersion;

                        if (installedVersion < Version.Parse(MainSetting.minVersion))
                        {
                            suggestedToUpdate = settings.DismissedUpdateVersion != onlineVersionStr;

                            if (onlineVersionStr != settings.DismissedUpdateVersion)
                            {
                                suggestedToUpdate = true;
                                settings.DismissedUpdateVersion = onlineVersionStr;
                            }
                        }

                        onlineVersionTextBlock.Text = resources["Version"] as string + " " + onlineVersionStr;
                        downloadUpdateButton.IsEnabled = true;

                        if (suggestedToUpdate)
                        {
                            showUpdateLog();
                        }
                        if (MainSetting.forceUpdate == true)
                        {
                            downloadUpdate(false, true);
                        }
                    }
                    else
                        onlineVersionTextBlock.Text = resources["OnlineVersionCheckFailed"] as string;
                }));
            };
        }

        void downloadUpdate(bool fullVersion = false, bool forceUpdate = false)
        {
            if (downloadingUpdate)
            {
                return;
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (!fullVersion && version == onlineVersionStr)
            {
                var result = MessageBox.Show(resources["SameVersionMessage"] as string, resources["XWallTitle"] as string, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;
            }

            if (updateDownloaded)
                if (forceUpdate == true)
                {
                    startUpdateInstalling(true);
                }
                else
                {
                    startUpdateInstalling(false);
                }

            else
            {
                downloadingUpdate = true;
                downloadUpdateButton.IsEnabled = false;

                var url = new Uri(Directory.Exists(App.AppDataDirectory + settings.GaFolderName) || fullVersion ? MainSetting.downloadUrlFull : MainSetting.downloadUrl);

                var client = new WebClient();
                //client.Proxy = null;

                client.DownloadFileAsync(url, App.AppDataDirectory + settings.ResourcesFolderName + settings.UpdateInstallerName);

                client.DownloadProgressChanged += (sender, e) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        onlineVersionTextBlock.Text = String.Format(resources["Downloading"] as string, e.ProgressPercentage);
                    }));
                };

                client.DownloadFileCompleted += (sender, e) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Operation.GrantAccessControl(App.AppDataDirectory + settings.ResourcesFolderName + settings.UpdateInstallerName);
                        downloadingUpdate = false;
                        downloadUpdateButton.IsEnabled = true;
                        onlineVersionTextBlock.Text = resources["Version"] as string + " " + onlineVersionStr;
                        if (e.Error == null)
                        {
                            updateDownloaded = true;
                            if (forceUpdate == true)
                            {
                                startUpdateInstalling(true);
                            }
                            else
                            {
                                startUpdateInstalling(false);
                            }
                        }
                        else
                            MessageBox.Show(resources["DownloadUpdateFailed"] as string);
                    }));
                };

                onlineVersionTextBlock.Text = String.Format(resources["Downloading"] as string, 0);
            }
        }

        void startUpdateInstalling(bool slient)
        {
            if (slient == false)
            {
                var result = MessageBox.Show(resources["InstallUpdateDescription"] as string, resources["XWallTitle"] as string, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    Process.Start(App.AppDataDirectory + settings.ResourcesFolderName + settings.UpdateInstallerName, "/silent");
                    App.Current.Shutdown();
                }
                else
                {
                    checkVersion();
                }
            }
            if (slient == true)
            {
                Process.Start(App.AppDataDirectory + settings.ResourcesFolderName + settings.UpdateInstallerName, "/silent");
                App.Current.Shutdown();
            }

        }

        void ruleCommandHandler(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Thread.Sleep(100);
                if (!File.Exists(e.FullPath)) return;
                if (e.Name == settings.NewRuleFileName)
                {
                    var rule = File.ReadAllText(e.FullPath);
                    Rules.CustomRules.Add(rule);
                    File.Delete(e.FullPath);
                }
                else if (e.Name == settings.DeleteRuleFileName)
                {
                    var rule = File.ReadAllText(e.FullPath);
                    Rules.CustomRules.Delete(rule);
                    File.Delete(e.FullPath);
                }
            }));
        }

        private void onDownloadUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            showUpdateLog();
        }

        private void onAboutTabItemGotFocus(object sender, RoutedEventArgs e)
        {
            if (onlineVersionStr == null)
                checkVersion();
        }

        private void onSshInfoPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (settings.ProxyType == "SSH" && !plink.IsConnected && plink.IsConnecting)
                {
                    onSshConnectButtonClick(sender, e);
                }
            }
        }

        private void onSshNewProfileButtonClick(object sender, RoutedEventArgs e)
        {
            Profile.SshProfile profile;

            var name = resources["Profile"] as string + " " + (sshProfiles.Items.Count + 1);

            if (sshProfilesListBox.SelectedIndex < 0)
            {
                profile = new Profile.SshProfile();
                profile.Server = settings.SshServer;
                profile.Port = settings.SshPort;
                profile.Username = settings.SshUsername;
                profile.Password = settings.SshPassword;
            }
            else
            {
                profile = new Profile.SshProfile(sshProfilesListBox.SelectedItem as Profile.SshProfile);
            }

            profile.Name = name;

            sshProfiles.Items.Add(profile);
            sshProfilesListBox.SelectedItem = profile;
            sshProfilesListBox.ScrollIntoView(profile);

            if (sshProfilesList.SelectedIndex < 0)
            {
                sshProfilesList.SelectedIndex = 0;
            }
        }

        private void onSshRemoveProfileButtonClick(object sender, RoutedEventArgs e)
        {
            var index = sshProfilesListBox.SelectedIndex;
            sshProfiles.Items.RemoveAt(index);
            sshProfilesListBox.SelectedIndex = Math.Min(index, sshProfilesListBox.Items.Count - 1);
        }

        private void onImportSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.FileName = "x-wall.config";
            dialog.FileOk += (s, eArgs) =>
            {
                var result = settings.Import(dialog.FileName);
                if (!result)
                {
                    MessageBox.Show(resources["FailedImportSettings"] as string);
                }
                else
                {
                    Process.Start(System.Windows.Forms.Application.ExecutablePath, "restart");
                    App.Current.Shutdown();
                }
            };
            dialog.ShowDialog();
        }

        private void onExportSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.FileName = "x-wall.config";
            dialog.FileOk += (s, eArgs) =>
            {
                var result = settings.Export(dialog.FileName);
                if (!result)
                {
                    MessageBox.Show(resources["FailedExportSettings"] as string);
                }
            };
            dialog.ShowDialog();
        }

        private void gaAppIdsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tooltip = gaAppIdsTextBox.ToolTip as ToolTip;
            tooltip.IsOpen = true;
        }

        private void gaAppIdsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tooltip = gaAppIdsTextBox.ToolTip as ToolTip;
            tooltip.IsOpen = false;
        }

        private void gaeWizardButton_Click(object sender, RoutedEventArgs e)
        {
            var wizard = new GAEWizard();
            wizard.Owner = this;
            wizard.ShowDialog();
        }
        private void SortAsFolderModifyTime(ref DirectoryInfo[] dirs)
        {
            Array.Sort(dirs, delegate(DirectoryInfo x, DirectoryInfo y) { return y.LastWriteTime.CompareTo(x.LastWriteTime); });
        }
        private void showUpdateLog()
        {
            var showUpdateLogWindow = new ShowUpdateLog();
            showUpdateLogWindow.Owner = this;
            showUpdateLogWindow.ShowDialog();
            if (MainSetting.isUpdateCancel == false)
            {
                downloadUpdate();
            }
            else
            {
                MainSetting.isUpdateCancel = false;
            }
        }


        private void button_sshkeyselect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "All File|*.*";
            if (openFileDialog.ShowDialog() == true)
            {

                sshPasswordBox.Text = openFileDialog.FileName;
                settings.SshPassword = sshPasswordBox.Text;
            }



        }
        private void checkbox_sshusekey_Checked(object sender, RoutedEventArgs e)
        {
            if (checkbox_sshusekey.IsChecked == true)
            {
                settings.SshUsePrivateKeyLogin = true;
                button_sshkeyselect.Visibility = System.Windows.Visibility.Visible;
                label_sshpwd.Content = resources["SshTypeKeyText"];
            }
        }

        private void checkbox_sshusekey_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkbox_sshusekey.IsChecked == false)
            {
                settings.SshUsePrivateKeyLogin = false;
                button_sshkeyselect.Visibility = System.Windows.Visibility.Hidden;
                label_sshpwd.Content = label_sshpwd.Content = resources["Password"]; ;

            }
        }

    }
    public class MainSetting
    {
        public static Boolean isUpdateCancel = false;
        public static string minVersion;
        public static string latestVersion;
        public static bool forceUpdate = false;
        public static string downloadUrl;
        public static string downloadUrlFull;
        public static string updateLog;
    }
}
