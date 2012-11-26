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
        App() { }

        protected override void OnStartup(StartupEventArgs eventArgs) {
            base.OnStartup(eventArgs);
            LoadLanguage();

            var executablePath = System.Windows.Forms.Application.ExecutablePath;
            Environment.CurrentDirectory = Path.GetDirectoryName(executablePath);

            var settings = Settings.Default;

            if (eventArgs.Args.Contains("uninstall")) {
                Operation.KillProcess(executablePath);
                Operation.KillProcess(settings.PrivoxyFileName);
                Operation.KillProcess(settings.PlinkFileName);
                Operation.Proxies.RestoreProxy();
                Operation.SetAutoStart(false);
                App.Current.Shutdown();
                return;
            }

            Process current = Process.GetCurrentProcess();
            var resources = App.Current.Resources;
            MessageBoxResult? result = null;
            foreach (Process process in Process.GetProcessesByName(current.ProcessName)) {
                if (process.Id != current.Id) {
                    if (result == null)
                        result = MessageBox.Show(resources["XWallAlreadyStartedDescription"] as string, resources["XWallTitle"] as string, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                        process.Kill();
                    else {
                        App.Current.Shutdown();
                        return;
                    }
                }
            }

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

        private void LoadLanguage() {
            CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;

            var files = new string[] { 
                @"Langs\" + currentCultureInfo.TwoLetterISOLanguageName + ".xaml",
                @"Langs\" + currentCultureInfo.Name + ".xaml"
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
