using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Deployment;
using XWall.Properties;
using System.Globalization;

namespace XWall {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        App() {
            Environment.CurrentDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);

            Process current = Process.GetCurrentProcess();
            MessageBoxResult? result = null;
            foreach (Process process in Process.GetProcessesByName(current.ProcessName)) {
                if (process.Id != current.Id) {
                    if (result == null)
                        result = MessageBox.Show(App.Current.Resources["XWallAlreadyStartedDescription"] as string, "X-Wall", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                        process.Kill();
                    else
                        App.Current.Shutdown();
                }
            }

            var settings = Settings.Default;
            if (settings.UpgradeRequired) {
                settings.Upgrade();
                settings.UpgradeRequired = false;
            }

            if (settings.FirstRun) {
                settings.FirstRun = false;

                //first time stuffs.
                Operation.SetAvailablePorts();
            }

            Operation.SetAutoStart(settings.AutoStart);
            Operation.Proxies.SetProxy("127.0.0.1:" + settings.ProxyPort);

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "ProxyPort": break;
                    default: return;
                }

                Operation.Proxies.SetProxy("127.0.0.1:" + settings.ProxyPort);
            };

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "AutoStart": break;
                    default: return;
                }

                Operation.SetAutoStart(settings.AutoStart);
            };
            //Operation.SetAvailablePorts();

            App.Current.Exit += (sender, e) => {
                Operation.Proxies.RestoreProxy();
            };
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            LoadLanguage();
        }

        private void LoadLanguage() {
            CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;

            var files = new string[] { 
                @"langs\" + currentCultureInfo.TwoLetterISOLanguageName + ".xaml",
                @"langs\" + currentCultureInfo.Name + ".xaml"
            };

            foreach (var file in files) {
                try {
                    var uri = new Uri(file, UriKind.Relative);
                    var dictionary = Application.LoadComponent(uri) as ResourceDictionary;
                    this.Resources.MergedDictionaries.Add(dictionary);
                } catch { }
            }
        }
    }
}
