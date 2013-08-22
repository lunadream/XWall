using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using XWall.Properties;

namespace XWall {
    class Privoxy {
        static Settings settings = Settings.Default;
        static ResourceDictionary resources = App.Current.Resources;
        Process process;
        bool stop;
        bool lastStopWithError = false;

        public Privoxy() {
            Operation.KillProcess(settings.PrivoxyFileName);
            App.Current.Exit += (sender, e) => {
                Stop();
            };

            settings.PropertyChanged += (sender, e) => {
                switch (e.PropertyName) {
                    case "ProxyPort": break;
                    case "ListenToLocalOnly": break;
                    case "UseIntranetProxy": break;
                    case "IntranetProxyServer": break;
                    case "IntranetProxyPort": break;
                    default: return;
                }

                GenerateConfigFile();
            };

            //if (!File.Exists(settings.PrivoxyConfigFileName))
            GenerateConfigFile();
        }

        public static void GenerateConfigFile() {
            /* !DEBUG CODE
            return;
            //*/
            //var defaultProxy = Operation.Proxies.DefaultProxy;
            var text =
                "listen-address " + (settings.ListenToLocalOnly ? "127.0.0.1:" : ":") + settings.ProxyPort + "\r\n" +
                "forwarded-connect-retries " + settings.ForwardConnectionRetries + "\r\n" +
                "forward / " + (settings.UseIntranetProxy ? (String.IsNullOrEmpty(settings.IntranetProxyServer) ? "127.0.0.1" : settings.IntranetProxyServer) + ":" + settings.IntranetProxyPort : ".") + "\r\n" +
                "keep-alive-timeout " + settings.PrivoxyKeepAliveTimeout + "\r\n" +
                "actionsfile " + App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyOnlineForwardActionFileName + "\r\n" +
                "actionsfile " + App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyOnlineDefaultActionFileName + "\r\n" +
                "actionsfile " + App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyCustomForwardActionFileName + "\r\n" +
                "actionsfile " + App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyCustomDefaultActionFileName + "\r\n" +
                "templdir " + App.AppDataDirectory + settings.ResourcesFolderName + settings.PrivoxyTemplatesFolderName + "\r\n";
            File.WriteAllText(App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyConfigFileName, text);
            Operation.GrantAccessControl(App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyConfigFileName);
        }

        void startProcess() {
            if (Environment.HasShutdownStarted) return;

            stop = false;
            process = new Process();

            var si = process.StartInfo;
            si.FileName = settings.PrivoxyFileName;
            si.Arguments = '"' + App.AppDataDirectory + settings.ConfigsFolderName + settings.PrivoxyConfigFileName + '"';
            si.CreateNoWindow = true;
            si.UseShellExecute = false;

            process.Start();
            process.WaitForExit();

            var code = process.HasExited ? process.ExitCode : 0;
            if (code == 1) {
                if (!lastStopWithError) {
                    lastStopWithError = true;
                    new Action(() => {
                        Thread.Sleep(1000);
                        MessageBox.Show(resources["PrivoxyErrorExitMessage"] as string);
                    }).BeginInvoke(null, null);
                }
            }
            else lastStopWithError = false;

            if (!stop) {
                new Action(() => {
                    Thread.Sleep(2000);
                    if (process == null || process.HasExited)
                        startProcess();
                }).BeginInvoke(null, null);
            }
        }

        public void Start() {
            Stop();
            new Action(startProcess).BeginInvoke(null, null);
        }

        public void Stop() {
            stop = true;
            try {
                process.Kill();
            }
            catch { }
        }

    }
}
