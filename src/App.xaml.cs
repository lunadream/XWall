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
using System.Text.RegularExpressions;
using System.Reflection;

namespace XWall {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static bool IsShutDown = false;
        public static string AppDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\X-Wall\";

        protected override void OnStartup(StartupEventArgs eventArgs) {
            base.OnStartup(eventArgs);
            LoadLanguage();

            var executablePath = System.Windows.Forms.Application.ExecutablePath;
            Environment.CurrentDirectory = Path.GetDirectoryName(executablePath);

            Directory.CreateDirectory(AppDataDirectory);
            Microsoft.Win32.SystemEvents.SessionEnding += (sender, e) => {
                IsShutDown = true;
                App.Current.Shutdown();
            };

            var settings = Settings.Default;

            if (eventArgs.Args.Length > 0) {
                var commandStr = eventArgs.Args[0];
                var match = new Regex(@"^(.*?)(?:/(.*))?$").Match(commandStr);
                var command = match.Groups[1].Value;
                var commandArg = match.Groups[2].Value;

                switch (command) {
                    case "uninstall":
                        Operation.KillProcess(executablePath);
                        Operation.KillProcess(settings.PrivoxyFileName);
                        Operation.KillProcess(settings.PlinkFileName);
                        Operation.Proxies.RestoreProxy();
                        Operation.SetAutoStart(false);
                        Operation.RegisterXWallProtocol(false);
                        IsShutDown = true;
                        App.Current.Shutdown();
                        return;
                    case "xwall:new-rule":
                        File.WriteAllText(App.AppDataDirectory + settings.NewRuleFileName, commandArg);
                        IsShutDown = true;
                        App.Current.Shutdown();
                        return;
                    case "xwall:del-rule":
                        File.WriteAllText(App.AppDataDirectory + settings.DeleteRuleFileName, commandArg);
                        IsShutDown = true;
                        App.Current.Shutdown();
                        return;
                    default:
                        break;
                }
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
                        IsShutDown = true;
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

            var autoStart = Operation.SetAutoStart(settings.AutoStart);
            if (autoStart != settings.AutoStart)
                settings.AutoStart = autoStart;

            Operation.Proxies.SetProxy("127.0.0.1:" + settings.ProxyPort);
            Operation.RegisterXWallProtocol(true);

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

                var rst = Operation.SetAutoStart(settings.AutoStart);
                if (rst != settings.AutoStart)
                    settings.AutoStart = rst;
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
