using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XWall.Properties;

namespace XWall {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GAEWizard : Window {
        static Settings settings = Settings.Default;
        static ResourceDictionary resources = App.Current.Resources;

        GAESimulator gaeSimulator = new GAESimulator();
        Action gaeSimulatorVerifyCallback;

        public GAEWizard() {
            InitializeComponent();

            prefixTextBox.Text = "ga-" + randomString();
            appsNumberTextBox.Text = Math.Max(settings.GaDefaultAppsNumber, settings.GaAppIds.Split('|').Length).ToString();
            currentState = homePanel;

            Loaded += (sender, e) => {
                Rules.Enabled = false;
            };

            Closed += (sender, e) => {
                Rules.Enabled = true;
                gaeSimulator.Stop();
            };
        }

        UIElement currentState;

        void showStage(UIElement ele) {
            if (ele != currentState) {
                currentState.Visibility = Visibility.Collapsed;
                currentState = ele;
                currentState.Visibility = Visibility.Visible;
            }
        }

        private void processButton_Click(object sender, RoutedEventArgs e) {
            var prefix = prefixTextBox.Text.Trim().ToLower();

            if (!new Regex(@"^[0-9a-z\-]{1,28}$").IsMatch(prefix)) {
                MessageBox.Show(resources["InvalidGaeAppIdPrefixMessage"] as string);
                return;
            }

            var email = emailTextBox.Text.Trim();

            if (!new Regex(@"^[0-9a-zA-Z\-.]+@[0-9a-zA-Z\-.]+$").IsMatch(email)) {
                MessageBox.Show(resources["InvalidGaeEmailMessage"] as string);
                return;
            }

            var password = passwordBox.Password;

            if (password.Length == 0) {
                MessageBox.Show(resources["EmptyGaePasswordMessage"] as string);
                return;
            }

            int appsNumber;

            var appsNumberParsed = int.TryParse(appsNumberTextBox.Text, out appsNumber);

            if (!(appsNumberParsed && appsNumber < 11 && appsNumber > 0)) {
                MessageBox.Show(resources["InvalidGaeAppsNumberMessage"] as string);
                return;
            }

            showStage(processingPanel);

            processingProgressBar.IsIndeterminate = true;
            processingStatusTextBlock.Text = resources["LoggingIn"] as string;

            gaeSimulator = new GAESimulator();

            gaeSimulator.Login(email, password, (success, data) => {
                if (!success) {
                    MessageBox.Show(resources["FailedToLoginMessage"] as string);
                    showStage(homePanel);
                    return;
                }

                gaeSimulator.CheckAppIdAvailability(prefix + "-1", (valid) => {
                    if (!valid) {
                        var r = MessageBox.Show(resources["InvalidGaeAppIdMessage"] as string, resources["XWall"] as string, MessageBoxButton.YesNo);
                        if (r == MessageBoxResult.Yes) {
                            prefix =
                            prefixTextBox.Text = "ga-" + randomString();
                        }
                        showStage(homePanel);
                        return;
                    }

                    var ids = new List<string>();
                    var noneDeployedIds = new List<string>(data.NoneDeployedAppIds);

                    if (data.AppIds.Length > 0) {
                        var r = MessageBox.Show(resources["GaeAppsCreatedMessage"] as string, resources["XWall"] as string, MessageBoxButton.YesNoCancel);
                        if (r == MessageBoxResult.Cancel) {
                            showStage(homePanel);
                            return;
                        }
                        else if (r == MessageBoxResult.Yes) {
                            ids.AddRange(data.AppIds.Take(appsNumber));
                        }
                    }

                    var createNumber = Math.Min(data.RemainingNumber, appsNumber - ids.Count);
                    
                    processingProgressBar.IsIndeterminate = false;
                    processingProgressBar.Value = 0;

                    gaeSimulator.VerificationRequired += (callback) => {
                        showStage(verifyPanel);
                        gaeSimulatorVerifyCallback = callback;
                    };

                    gaeSimulator.CreateApps(prefix, createNumber, (done, i, id, error) => {
                        if (error) {
                            showStage(homePanel);
                            return;
                        }

                        if (!done) {
                            processingStatusTextBlock.Text = String.Format(resources["CreatingGaeApps"] as string, i + 1, createNumber);
                            setProgressBarValue(processingProgressBar, i * 1.0 / createNumber * 20);

                            ids.Add(id);
                            noneDeployedIds.Add(id);
                        }
                        else {
                            string[] idsArr;
                            if (
                                data.AppIds.Length != data.NoneDeployedAppIds.Length &&
                                MessageBox.Show(resources["DeployDeployedGaeAppsMessage"] as string, resources["XWall"] as string, MessageBoxButton.YesNo) == MessageBoxResult.Yes
                            ) {
                                idsArr = ids.ToArray();
                            }
                            else { 
                                idsArr = (
                                    from appId in ids
                                    where noneDeployedIds.Contains(appId)
                                    select appId
                                ).ToArray();
                            }

                            gaeSimulator.Deploy(email, password, idsArr, (dDone, dI, currentProgress, err) => {
                                Dispatcher.Invoke(new Action(() => {
                                    if (err) {
                                        showStage(homePanel);
                                        return;
                                    }

                                    if (!dDone) {
                                        processingStatusTextBlock.Text = String.Format(resources["DeployingGaeApps"] as string, dI + 1, idsArr.Length);
                                        setProgressBarValue(processingProgressBar, (dI + currentProgress) / idsArr.Length * 80 + 20);
                                    }
                                    else {
                                        processingStatusTextBlock.Text = resources["GaeWizardFinished"] as string;
                                        setProgressBarValue(processingProgressBar, 100);
                                        settings.GaAppIds = string.Join("|", ids.ToArray());
                                        Rules.Enabled = true;
                                        new Action(() => {
                                            Thread.Sleep(3000);
                                            Dispatcher.BeginInvoke(new Action(() => {
                                                Close();
                                            }));
                                        }).BeginInvoke(null, null);
                                    }
                                }));
                            });
                        }
                    });
                });
            });
        }

        void setProgressBarValue(ProgressBar progressBar, double value) {
            var duration = TimeSpan.FromMilliseconds(500);
            var animation = new DoubleAnimation(value, duration);
            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        private string randomString(int n = 8) {
            var chrs = "abcdefghijklmnopqrstuvwxyz1234567890";
            var str = "";
            var rnd = new Random();
            for (int i = 0; i < n; i++) {
                str += chrs[rnd.Next(chrs.Length)];
            }
            return str;
        }

        private void sendVerificationButton_Click(object sender, RoutedEventArgs e) {
            var method = (bool)smsRadio.IsChecked ? "SMS" : "CTC";
            var country = "CN";
            var phone = verifyPhoneTextBox.Text.Trim();

            if (!new Regex(@"^\d{11}$").IsMatch(phone)) {
                MessageBox.Show(resources["GaeInvalidPhoneMessage"] as string);
                return;
            }

            sendVerifierButton.IsEnabled = false;
            sendVerifierButton.Content = resources["Sending"] as string;

            gaeSimulator.SendVerifier(method, country, phone, (success) => {
                if (success) {
                    verifierTextBox.Focus();
                    verifyButton.IsEnabled = true;
                }
                else {
                    MessageBox.Show(resources["GaeFailToSendVerifierMessage"] as string);
                }
                sendVerifierButton.IsEnabled = true;
                sendVerifierButton.Content = resources["Send"] as string;
            });
        }

        private void verifyButton_Click(object sender, RoutedEventArgs e) {
            var verifier = verifierTextBox.Text.Trim();

            if (verifier == "") {
                MessageBox.Show(resources["GaeEmptyVerifierMessage"] as string);
                return;
            }

            verifyButton.IsEnabled = false;
            sendVerifierButton.IsEnabled = false;
            verifyButton.Content = resources["Verifying"] as string;

            gaeSimulator.Verify(verifier, (success) => {
                if (success) {
                    gaeSimulatorVerifyCallback();
                    showStage(processingPanel);
                }
                else {
                    MessageBox.Show(resources["GaeInvalidVerifierMessage"] as string);
                }

                verifierTextBox.Text = "";
                verifyButton.IsEnabled = true;
                sendVerifierButton.IsEnabled = true;
                verifyButton.Content = resources["Verify"] as string;
            });
        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && processButton.IsEnabled) {
                processButton_Click(null, null);
            }
        }

        private void verifyPhoneTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && sendVerifierButton.IsEnabled) {
                sendVerificationButton_Click(null, null);
            }
        }

        private void verifierTextBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && verifyButton.IsEnabled) {
                verifyButton_Click(null, null);
            }
        }

    }
}
